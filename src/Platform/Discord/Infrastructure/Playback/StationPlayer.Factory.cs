using System.Collections.Concurrent;
using System.Threading.Channels;
using Open.ChannelExtensions;
using Wadio.Extensions.Icecast;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class StationPlayerFactory(
    IWadioApi api,
    IcecastClient icecast,
    ILogger<StationPlayerFactory> logger,
    Channel<StationPlayerFactory.CreateAction> queue ) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, StationPlayerEntry> players = new();

    public async Task<StationPlayer> Create( Guid stationId, CancellationToken cancellation = default )
    {
        var request = new CreateAction( stationId );
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
            var reader = queue.PipeAsync(
                Environment.ProcessorCount,
                action => OnLoadStation( api.Stations, action, cancellation ),
                Environment.ProcessorCount * 2,
                false,
                cancellation ).PipeAsync(
                Environment.ProcessorCount,
                OnRejectHls,
                Environment.ProcessorCount * 2,
                false,
                cancellation );

            while( !cancellation.IsCancellationRequested )
            {
                var result = await reader.ReadAsync( cancellation );
                if( result?.IsCompleted is null or true )
                {
                    continue;
                }

                var player = await GetOrAddPlayer(
                    result.Station,
                    cancellation );

                result.Completion.TrySetResult( player );
            }

            static async ValueTask<StationResult?> OnLoadStation( IStationsApi stations, CreateAction action, CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( stations );
                ArgumentNullException.ThrowIfNull( action );

                try
                {
                    var station = await stations.Get( action.StationId, cancellation );
                    if( station is null )
                    {
                        action.Completion.TrySetException( new ArgumentException( $"Station '{action.StationId}' does not exist.", nameof( action ) ) );
                        return default;
                    }

                    return new( action, station );
                }
                catch( Exception e )
                {
                    action.Completion.TrySetException( e );
                    return default;
                }
            }

            static ValueTask<StationResult?> OnRejectHls( StationResult? context )
            {
                if( context?.IsCompleted is null or true )
                {
                    return default;
                }

                if( context.Station.IsHls )
                {
                    context.Completion.TrySetException( new ArgumentException( $"Station '{context.Station.Id}' is not supported. (IsHls=true)", nameof( context ) ) );
                    return default;
                }

                return new( context );
            }
        }
    }

    private async ValueTask<StationPlayer> GetOrAddPlayer( Station station, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( station );
        if( station.IsHls )
        {
            throw new ArgumentException( $"Station '{station.Id}' is not supported. (IsHls=true)", nameof( station ) );
        }

        if( players.TryGetValue( station.Id, out var entry )
            &&
            players.TryUpdate( station.Id, entry with { Count = entry.Count + 1 }, entry ) )
        {
            logger.OnRetrievedPlayer( station.Id );
            return entry.Value;
        }

        var player = await CreateIcecastPlayer(
            station,
            cancellation );

        entry = players.AddOrUpdate(
            station.Id,
            stationId => new( 1, player ),
            ( _, value ) => value with { Count = value.Count + 1 } );

        if( entry.Value != player )
        {
            await player.DisposeAsync();
        }

        logger.OnCreatedPlayer( station.Id );
        return entry.Value;

        async Task<IcecastStationPlayer> CreateIcecastPlayer( Station station, CancellationToken cancellation )
        {
            var reader = await icecast.GetReader(
                station.Url,
                cancellation );

            logger.OnCreatedIcecastReader( station.Id, station.Url );
            return new( reader, players, station );
        }
    }

    internal sealed record CreateAction( Guid StationId )
    {
        public TaskCompletionSource<StationPlayer> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
    };

    private sealed class StationResult( CreateAction action, Station station )
    {
        public TaskCompletionSource<StationPlayer> Completion => action.Completion;
        public bool IsCompleted => Completion.Task.IsCompleted;
        public Station Station => station;
    }
}

internal sealed record StationPlayerEntry( int Count, StationPlayer Value ) : IAsyncDisposable
{
    public ValueTask DisposeAsync( ) => Value.DisposeAsync();
}

internal static partial class StationPlayerFactoryLogging
{
    [LoggerMessage( Level = LogLevel.Debug, Message = "Created Icecast Reader for Station '{stationId}', '{stationUrl}'" )]
    public static partial void OnCreatedIcecastReader( this ILogger<StationPlayerFactory> logger, Guid stationId, Uri stationUrl );

    [LoggerMessage( Level = LogLevel.Information, Message = "Created Player for Station '{stationId}'" )]
    public static partial void OnCreatedPlayer( this ILogger<StationPlayerFactory> logger, Guid stationId );

    [LoggerMessage( Level = LogLevel.Debug, Message = "Retrieved Player for Station '{stationId}'" )]
    public static partial void OnRetrievedPlayer( this ILogger<StationPlayerFactory> logger, Guid stationId );
}