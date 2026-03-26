using System.Collections.Concurrent;
using System.Threading.Channels;
using Open.ChannelExtensions;
using Wadio.Extensions.Icecast;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class StationPlayerFactory(
    IWadioApi api,
    IcecastClient icecast,
    Channel<StationPlayerFactory.CreatePlayerRequest> queue ) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, StationPlayerEntry> players = new();

    public async Task<StationPlayer> Create( Guid stationId, CancellationToken cancellation = default )
    {
        var request = new CreatePlayerRequest( stationId );
        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync(
                request,
                cancellation );

            return await request.Completion.Task.ConfigureAwait( false );
        }
    }

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        try
        {
            await Execute( cancellation );
        }
        finally
        {
            await Disposer.DisposeAsync( players );
        }

        async ValueTask Execute( CancellationToken cancellation )
        {
            var reader = queue.Pipe( Environment.ProcessorCount, request => new PipelineContext( request ), -1, false, cancellation )
                .PipeAsync( Environment.ProcessorCount, LoadStation, -1, false, cancellation )
                .Filter( context => !context.IsCompleted )
                .PipeAsync( Environment.ProcessorCount, ResolvePlayer, -1, false, cancellation )
                .Filter( context => !context.IsCompleted );

            while( !cancellation.IsCancellationRequested )
            {
                var (request, player) = await reader.ReadAsync( cancellation );
                if( player is null )
                {
                    continue;
                }

                request.Completion.SetResult( player );
            }

            ValueTask<PipelineContext<StationPlayer?>> ResolvePlayer( PipelineContext<Station?> context ) => context.Invoke(
                async ( ) => await GetOrAddPlayer( context.Value!.Id, cancellation ),
                cancellation );

            ValueTask<PipelineContext<Station?>> LoadStation( PipelineContext context ) => context.Invoke(
                async ( ) => await api.Stations.Get( context.Request.StationId, cancellation ) ?? throw new InvalidOperationException( $"Station '{context.Request.StationId}' does not exist." ),
                cancellation );
        }
    }

    private async ValueTask<StationPlayer> GetOrAddPlayer( Guid stationId, CancellationToken cancellation )
    {
        if( players.TryGetValue( stationId, out var entry )
            &&
            players.TryUpdate( stationId, entry with { Count = entry.Count + 1 }, entry ) )
        {
            return entry.Value;
        }

        var station = await api.Stations.Get( stationId, cancellation );
        var player = station switch
        {
            null => throw new ArgumentException( $"Station '{stationId}' does not exist.", nameof( stationId ) ),
            { IsHls: true } => throw new ArgumentException( $"Station '{stationId}' is not supported. (IsHls=true)", nameof( stationId ) ),

            _ => await CreateIcecastPlayer( station, cancellation ),
        };

        entry = players.AddOrUpdate(
            station.Id,
            stationId => new( 1, player ),
            ( _, value ) => value with { Count = value.Count + 1 } );

        if( entry.Value != player )
        {
            await player.DisposeAsync();
        }

        return entry.Value;

        async Task<IcecastStationPlayer> CreateIcecastPlayer( Station station, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( station );
            if( station.IsHls )
            {
                throw new ArgumentException( $"Station '{stationId}' is not supported. (IsHls=true)", nameof( stationId ) );
            }

            var reader = await icecast.GetReader(
                station.Url,
                cancellation );

            return new( reader, players, station );
        }
    }

    internal sealed record CreatePlayerRequest( Guid StationId )
    {
        public TaskCompletionSource<StationPlayer> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
    };
}

file record PipelineContext( StationPlayerFactory.CreatePlayerRequest Request )
{
    public bool IsCompleted => Request.Completion.Task.IsCompleted;

    public async ValueTask<PipelineContext<TNext?>> Invoke<TNext>( Func<ValueTask<TNext>> work, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( work );

        using( cancellation.Register( ( ) => Request.Completion.TrySetCanceled( cancellation ) ) )
        {
            try
            {
                var value = await work().ConfigureAwait( false );
                return new( Request, value );
            }
            catch( Exception e )
            {
                Request.Completion.TrySetException( e );
                return new( Request );
            }
        }
    }
}

file record PipelineContext<T>( StationPlayerFactory.CreatePlayerRequest Request, T? Value = default ) : PipelineContext( Request )
{
    public static (StationPlayerFactory.CreatePlayerRequest Request, T? Value) Deconstruct( PipelineContext<T> context ) => (context.Request, context.Value);
}

internal sealed record StationPlayerEntry( int Count, StationPlayer Value ) : IAsyncDisposable
{
    public ValueTask DisposeAsync( ) => Value.DisposeAsync();
}