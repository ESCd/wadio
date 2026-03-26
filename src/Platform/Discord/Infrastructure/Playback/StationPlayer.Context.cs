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
    StationPlayerRenderer renderer,
    Channel<StationPlayerAction> queue ) : BackgroundService
{
    private readonly PcmEncoderPool encoders = new( codec => new FFmpegPcmEncoder( codec ) );
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
                var request = await queue.Reader.ReadAsync( cancellation );
                using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
                {
                    try
                    {
                        await OnProcessRequest(
                            request,
                            cancellation );
                    }
                    catch( Exception e )
                    {
                        request.Completion.TrySetException( e );
                    }
                }
            }
        }
    }

    private ValueTask OnProcessRequest( StationPlayerAction request, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( request );

        return request switch
        {
            StationPlayerAction.Play connect => OnConnect( connect, cancellation ),
            StationPlayerAction.Stop disconnect => OnDisconnect( disconnect, cancellation ),

            _ => throw new ArgumentException( $"{nameof( StationPlayerAction )} of type '{request.GetType().FullName}' is not supported.", nameof( request ) )
        };

        async ValueTask OnConnect( StationPlayerAction.Play request, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( request );

            if( store.TryGetValue( request.Channel.GuildId, out var controller ) )
            {
                if( controller.ChannelId == request.Channel.Id && controller.StationId != request.StationId )
                {
                    await UpdateStation(
                        factory,
                        request,
                        controller,
                        cancellation );

                    return;
                }

                controller = default;
            }

            // NOTE: disconnect the player
            await OnDisconnect( new( request.Channel.GuildId ), cancellation );

            var voice = await CreateVoice(
                request.Channel.GuildId,
                request.Channel.Id,
                cancellation );

            controller = store.Update(
                request.Channel.GuildId,
                new( encoders, gateway, renderer, store, voice ) );

            await UpdateStation(
                factory,
                request,
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
#if DEBUG
                        Logger = new ConsoleLogger()
#endif
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

        async ValueTask OnDisconnect( StationPlayerAction.Stop request, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( request );

            if( !await store.RemoveAsync( request.GuildId ) )
            {
                await gateway.UpdateVoiceStateAsync(
                    new( request.GuildId, default ),
                    default,
                    cancellation );
            }

            request.Completion.SetResult();
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