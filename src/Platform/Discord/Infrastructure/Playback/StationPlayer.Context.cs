using System.Threading.Channels;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class StationPlayerContext(
    StationPlayerFactory factory,
    GatewayClient gateway,
    ILoggerFactory loggerFactory,
    StationPlayerRenderer renderer,
    Channel<StationPlayerAction> queue ) : BackgroundService
{
    private readonly PcmEncoderPool encoders = new( codec => new FFmpegPcmEncoder( codec ) );
    private readonly ILogger<StationPlayerContext> logger = loggerFactory.CreateLogger<StationPlayerContext>();
    private readonly StationPlayerStore store = new();

    private async Task Dispatch( StationPlayerAction request, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( request );

        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync( request, cancellation );
            await request.Completion.Task.ConfigureAwait( false );
        }
    }

    private async Task<TResult> Dispatch<TResult>( StationPlayerAction<TResult> request, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( request );

        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync( request, cancellation );
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
            await Disposer.DisposeAsync(
                encoders,
                store );
        }

        async ValueTask Execute( CancellationToken cancellation )
        {
            while( !cancellation.IsCancellationRequested )
            {
                var action = await queue.Reader.ReadAsync( cancellation );
                using( cancellation.Register( ( ) => action.Completion.TrySetCanceled( cancellation ) ) )
                {
                    try
                    {
                        await OnProcessAction(
                            action,
                            cancellation );
                    }
                    catch( Exception e )
                    {
                        action.Completion.TrySetException( e );
                    }
                }
            }
        }
    }

    private ValueTask OnProcessAction( StationPlayerAction action, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( action );

        logger.OnProcessingAction( action );
        return action switch
        {
            StationPlayerAction.Play connect => OnConnect( connect, cancellation ),
            StationPlayerAction.Stop disconnect => OnDisconnect( disconnect, cancellation ),

            _ => throw new ArgumentException( $"{nameof( StationPlayerAction )} of type '{action.GetType().FullName}' is not supported.", nameof( action ) )
        };

        async ValueTask OnConnect( StationPlayerAction.Play action, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( action );

            if( store.TryGetValue( action.Channel.GuildId, out var controller ) )
            {
                if( controller.ChannelId == action.Channel.Id && controller.StationId != action.StationId )
                {
                    await UpdateStation(
                        factory,
                        action,
                        controller,
                        cancellation );

                    return;
                }

                controller = default;
            }

            // NOTE: disconnect the player
            await OnDisconnect( new( action.Channel.GuildId ), cancellation );

            var voice = await CreateVoice(
                action.Channel.GuildId,
                action.Channel.Id,
                cancellation );

            controller = await store.Update( action.Channel.GuildId, new(
                encoders,
                gateway,
                loggerFactory.CreateLogger<StationPlayerController>(),
                renderer,
                store,
                voice ) );

            await UpdateStation(
                factory,
                action,
                controller,
                cancellation );

            async Task<VoiceClient> CreateVoice( ulong guildId, ulong channelId, CancellationToken cancellation )
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero( channelId );

                var voice = await gateway.JoinVoiceChannelAsync(
                    guildId,
                    channelId,
                    new()
                    {
                        Logger = new VoiceLoggingAdapter(
                            loggerFactory.CreateLogger<VoiceLoggingAdapter>(),
                            (guildId, channelId) ),
                    },
                    cancellation );

                await voice.StartAsync( cancellation );
                await voice.EnterSpeakingStateAsync(
                    new( SpeakingFlags.Microphone ),
                    default,
                    cancellation );

                return voice;
            }

            static async ValueTask UpdateStation(
                StationPlayerFactory factory,
                StationPlayerAction.Play request,
                StationPlayerController controller,
                CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( factory );
                ArgumentNullException.ThrowIfNull( request );
                ArgumentNullException.ThrowIfNull( controller );

                var status = await controller.Play(
                    await factory.Create( request.StationId, cancellation ),
                    cancellation );

                var message = await request.OnReady( status );
                controller.AddOutput( message );

                request.Completion.SetResult( message );
            }
        }

        async ValueTask OnDisconnect( StationPlayerAction.Stop action, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( action );

            if( !await store.RemoveAsync( action.GuildId ) )
            {
                await gateway.UpdateVoiceStateAsync(
                    new( action.GuildId, default ),
                    default,
                    cancellation );
            }

            action.Completion.SetResult();
        }
    }

    public Task<RestMessage> Play(
        IVoiceGuildChannel channel,
        Guid stationId,
        StationPlayerBindingFactory onReady,
        CancellationToken cancellation = default )
        => Dispatch( new StationPlayerAction.Play( channel, onReady, stationId ), cancellation );

    public async ValueTask<RestMessage?> Status(
        ulong guildId,
        StationPlayerBindingFactory onReady,
        CancellationToken cancellation = default )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );

        if( store.TryGetValue( guildId, out var controller ) )
        {
            var status = await controller.Status( cancellation );

            var message = await onReady( status );
            controller.AddOutput( message );

            return message;
        }

        return await onReady( default );
    }

    public Task Stop( ulong guildId, CancellationToken cancellation = default ) => Dispatch( new StationPlayerAction.Stop( guildId ), cancellation );
}

internal abstract record StationPlayerAction
{
    public TaskCompletionSource Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );

    public sealed record Play( IVoiceGuildChannel Channel, StationPlayerBindingFactory OnReady, Guid StationId ) : StationPlayerAction<RestMessage>;
    public sealed record Stop( ulong GuildId ) : StationPlayerAction;
}

internal abstract record StationPlayerAction<TResult> : StationPlayerAction
{
    public new TaskCompletionSource<TResult> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
}

internal static partial class StationPlayerContextLogging
{
    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Processing: {action}" )]
    public static partial void OnProcessingAction( this ILogger<StationPlayerContext> logger, StationPlayerAction action );
}

internal sealed partial class VoiceLoggingAdapter( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId ) : IVoiceLogger
{
    public void Log<TState>( NetCord.Logging.LogLevel level, TState state, Exception? exception, Func<TState, Exception?, string> formatter )
    {
        ArgumentNullException.ThrowIfNull( formatter );

        if( !IsEnabled( level ) || level is NetCord.Logging.LogLevel.None )
        {
            return;
        }

        switch( level )
        {
            case NetCord.Logging.LogLevel.Critical:
#pragma warning disable CA1873
                OnCritical( logger, voiceId, formatter( state, exception ), exception );
                return;

            case NetCord.Logging.LogLevel.Debug:
                OnDebug( logger, voiceId, formatter( state, exception ), exception );
                return;

            case NetCord.Logging.LogLevel.Error:
                OnError( logger, voiceId, formatter( state, exception ), exception );
                return;

            case NetCord.Logging.LogLevel.Information:
                OnInformation( logger, voiceId, formatter( state, exception ), exception );
                return;

            case NetCord.Logging.LogLevel.Trace:
                OnTrace( logger, voiceId, formatter( state, exception ), exception );
                return;

            case NetCord.Logging.LogLevel.Warning:
                OnWarning( logger, voiceId, formatter( state, exception ), exception );
                return;
#pragma warning restore CA1873

            default: return;
        }
    }

    public bool IsEnabled( NetCord.Logging.LogLevel level ) => logger.IsEnabled( Convert( level ) );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Critical, Message = "{voiceId} {message}" )]
    private static partial void OnCritical( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{voiceId} {message}" )]
    private static partial void OnDebug( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "{voiceId} {message}" )]
    private static partial void OnError( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "{voiceId} {message}" )]
    private static partial void OnInformation( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Trace, Message = "{voiceId} {message}" )]
    private static partial void OnTrace( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    [LoggerMessage( Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "{voiceId} {message}" )]
    private static partial void OnWarning( ILogger<VoiceLoggingAdapter> logger, (ulong GuildId, ulong ChannelId) voiceId, string message, Exception? exception );

    private static Microsoft.Extensions.Logging.LogLevel Convert( NetCord.Logging.LogLevel level ) => level switch
    {
        NetCord.Logging.LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
        NetCord.Logging.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
        NetCord.Logging.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
        NetCord.Logging.LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
        NetCord.Logging.LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
        NetCord.Logging.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
        _ => Microsoft.Extensions.Logging.LogLevel.None
    };
}