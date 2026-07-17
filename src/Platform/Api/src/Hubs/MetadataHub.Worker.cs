using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Open.ChannelExtensions;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.Icecast.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Client.Abstractions;

namespace Wadio.Platform.Api.Hubs;

internal sealed class MetadataHubWorker(
    IWadioApi api,
    IMetadataWorkerContext context,
    IcecastClient icecast,
    ILogger<MetadataHubWorker> logger,
    IMetadataSampler sampler ) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, MetadataReaderValue> readers = [];

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        try
        {
            await Execute( cancellation );
        }
        finally
        {
            foreach( var value in readers.Values )
            {
                await value.Reader.DisposeAsync();
            }

            readers.Clear();
        }

        async ValueTask Execute( CancellationToken cancellation )
        {
            while( !cancellation.IsCancellationRequested )
            {
                var request = await context.Next( cancellation );
                await using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
                {
                    ReaderSubscription? subscription = default;
                    try
                    {
                        subscription = await OnSubscribe(
                            request.StationId,
                            cancellation );

                        request.Completion.SetResult( subscription );
                    }
                    catch( Exception e )
                    {
                        request.Completion.TrySetException( e );
                        if( subscription is not null )
                        {
                            await subscription.DisposeAsync();
                        }

                        logger.OnFailedToSubscribe( request.StationId, e );
                        continue;
                    }
                }
            }
        }
    }

    private async ValueTask<ReaderSubscription> OnSubscribe( Guid stationId, CancellationToken cancellation )
    {
        if( readers.TryGetValue( stationId, out var value )
            &&
            readers.TryUpdate( stationId, value with { Count = value.Count + 1 }, value ) )
        {
            return new( value.Reader, readers, sampler, stationId );
        }

        var station = await api.Stations.Get(
            stationId,
            cancellation ) ?? throw new ArgumentException( $"Station '{stationId}' does not exist.", nameof( stationId ) );

        if( station.IsHls )
        {
            throw new ArgumentException( $"Station '{station.Id}' is not supported. (IsHls=true)", nameof( stationId ) );
        }

        var reader = await icecast.GetReader(
            station.Url,
            cancellation );

        value = readers.AddOrUpdate(
            stationId,
            stationId => new( reader, 1 ),
            ( _, value ) => value with { Count = value.Count + 1 } );

        if( value.Reader != reader )
        {
            await reader.DisposeAsync();
        }

        return new( reader, readers, sampler, stationId );
    }

    private sealed class ReaderSubscription(
        IcecastStreamReader reader,
        ConcurrentDictionary<Guid, MetadataReaderValue> readers,
        IMetadataSampler sampler,
        Guid stationId ) : IMetadataWorkerSubscription
    {
        private IcecastMetadataDictionary? value;

        public async ValueTask DisposeAsync( )
        {
            if( readers.TryGetValue( stationId, out var entry ) )
            {
                var updated = entry with
                {
                    Count = Math.Max( 0, entry.Count - 1 )
                };

                if( readers.TryUpdate( stationId, updated, entry ) )
                {
                    if( updated.Count is 0 && readers.Remove( stationId, out var removed ) )
                    {
                        await removed.Reader.DisposeAsync();
                        if( removed.Reader != reader )
                        {
                            await reader.DisposeAsync();
                        }
                    }
                }
            }
        }

        public async IAsyncEnumerable<IcecastMetadataDictionary?> Read( [EnumeratorCancellation] CancellationToken cancellation )
        {
            while( !cancellation.IsCancellationRequested )
            {
                var metadata = await MoveNext( reader, cancellation );
                if( metadata is null )
                {
                    yield break;
                }

                if( value != metadata )
                {
                    value = metadata;
                    yield return metadata;

                    await sampler.Sample( new( reader.Url, stationId, MetadataType.Icecast )
                    {
                        Data = metadata,
                    }, cancellation );
                }
            }

            static async ValueTask<IcecastMetadataDictionary?> MoveNext( IcecastStreamReader reader, CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( reader );

                try
                {
                    return await reader.WaitUntilMetadata( cancellation ).ConfigureAwait( false );
                }
                catch( Exception e ) when( e is EndOfStreamException or OperationCanceledException )
                {
                    return default;
                }
            }
        }
    }

    private sealed record MetadataReaderValue( IcecastStreamReader Reader, ulong Count );
}

internal sealed class MetadataWorkerContext : IAsyncDisposable, IMetadataWorkerContext
{
    private readonly Channel<MetadataWorkerRequest> queue = Channel.CreateBounded<MetadataWorkerRequest>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
    } );

    public async ValueTask DisposeAsync( )
    {
        await queue.CompleteAsync( new ObjectDisposedException( nameof( MetadataWorkerContext ) ) );
    }

    public ValueTask<MetadataWorkerRequest> Next( CancellationToken cancellation ) => queue.Reader.ReadAsync( cancellation );

    public async Task<IMetadataWorkerSubscription> Subscribe( Guid stationId, CancellationToken cancellation )
    {
        var request = new MetadataWorkerRequest( stationId );
        await using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync(
                request,
                cancellation );

            return await request.Completion.Task.ConfigureAwait( false );
        }
    }
}

internal static partial class MetadataWorkerLogging
{
    [LoggerMessage( Level = LogLevel.Error, Message = "Failed to Subscribe to Station '{stationId}'." )]
    public static partial void OnFailedToSubscribe( this ILogger<MetadataHubWorker> logger, Guid stationId, Exception? exception );
}