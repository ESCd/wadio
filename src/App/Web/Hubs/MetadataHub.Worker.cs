using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Wadio.App.Abstractions.Api;
using Wadio.App.Abstractions.Signals;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.App.Web.Hubs;

internal sealed class MetadataHubWorker(
    IWadioApi api,
    IMetadataWorkerContext context,
    IHubContext<MetadataHub> hub,
    IcecastMetadataClient icecast ) : BackgroundService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, MetadataReaderValue> readers = [];

#pragma warning disable CA2215 // we implement `IAsyncDisposable`
    public override void Dispose( )
#pragma warning restore CA2215
    {
    }

    public async ValueTask DisposeAsync( )
    {
        base.Dispose();
        foreach( var value in readers.Values )
        {
            await value.Reader.DisposeAsync();
        }

        readers.Clear();
    }

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        while( !cancellation.IsCancellationRequested )
        {
            var request = await context.Read( cancellation );
            using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
            {
                try
                {
                    await OnSubscribe( request.StationId, cancellation );
                }
                catch( Exception e )
                {
                    request.Completion.TrySetException( e );
                    continue;
                }

                request.Completion.SetResult( new ReaderSubscription( readers, request.StationId ) );
            }
        }
    }

    private async ValueTask OnSubscribe( Guid stationId, CancellationToken cancellation )
    {
        if( readers.TryGetValue( stationId, out var entry )
            &&
            readers.TryUpdate( stationId, entry with { Count = entry.Count + 1 }, entry ) )
        {
            return;
        }

        var station = await api.Stations.Get( stationId, cancellation ) ?? throw new ArgumentException( $"The station '{stationId}' does not exist.", nameof( stationId ) );
        if( station.IsHls )
        {
            throw new ArgumentException( $"The station '{stationId}' is not supported (IsHls=true).", nameof( stationId ) );
        }

        var reader = await icecast.GetReader(
            station.Url,
            cancellation );

        if( readers.AddOrUpdate( stationId, stationId =>
        {
            reader.MetadataRead += OnMetadata;
            return new( reader, 1 );
        }, ( _, value ) => value with { Count = value.Count + 1 } ).Reader != reader )
        {
            reader.MetadataRead -= OnMetadata;
            await reader.DisposeAsync();
        }

        async ValueTask OnMetadata( IcecastMetadataDictionary metadata ) => await hub.Clients.Group( stationId.ToString() ).SendAsync(
            nameof( MetadataSignals.Metadata ),
            metadata,
            CancellationToken.None );
    }

    private sealed class ReaderSubscription(
        ConcurrentDictionary<Guid, MetadataReaderValue> readers,
        Guid stationId ) : IMetadataWorkerSubscription
    {
        public Guid StationId => stationId;

        public ValueTask DisposeAsync( )
        {
            if( readers.TryGetValue( stationId, out var entry ) )
            {
                var updated = entry with
                {
                    Count = Math.Max( 0, entry.Count - 1 )
                };

                if( readers.TryUpdate( stationId, updated, entry ) && updated.Count is 0 )
                {
                    readers.Remove( stationId, out _ );
                    return entry.Reader.DisposeAsync();
                }
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed record MetadataReaderValue( IcecastMetadataReader Reader, ulong Count );
}

public interface IMetadataWorkerContext
{
    internal ValueTask<MetadataWorkerRequest> Read( CancellationToken cancellation );

    public Task<IMetadataWorkerSubscription> Subscribe( Guid stationId, CancellationToken cancellation );
}

public interface IMetadataWorkerSubscription : IAsyncDisposable
{
    public Guid StationId { get; }
}

internal sealed record MetadataWorkerRequest( Guid StationId )
{
    public TaskCompletionSource<IMetadataWorkerSubscription> Completion { get; } = new();
}

internal sealed class MetadataWorkerContext : IMetadataWorkerContext
{
    private readonly Channel<MetadataWorkerRequest> channel = Channel.CreateBounded<MetadataWorkerRequest>( new BoundedChannelOptions( 1024 )
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
    } );

    public async Task<IMetadataWorkerSubscription> Subscribe( Guid stationId, CancellationToken cancellation )
    {
        var request = new MetadataWorkerRequest( stationId );
        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await channel.Writer.WriteAsync(
                request,
                cancellation );

            return await request.Completion.Task.ConfigureAwait( false );
        }
    }

    public ValueTask<MetadataWorkerRequest> Read( CancellationToken cancellation ) => channel.Reader.ReadAsync( cancellation );
}