using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;

namespace Wadio.Platform.Discord.Infrastructure;

internal sealed class StationPlayerWorker(
    StationPlayerFactory factory,
    GatewayClient gateway,
    Channel<StationPlayerRequest> queue ) : BackgroundService
{
    private readonly ConcurrentDictionary<ulong, StationPlayerContext> contexts = new();

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        while( !cancellation.IsCancellationRequested )
        {
            var request = await queue.Reader.ReadAsync( cancellation );
            using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
            {
                try
                {
                    await AddOrUpdatePlayer(
                        request,
                        cancellation );

                    request.Completion.SetResult();
                }
                catch( Exception e )
                {
                    request.Completion.TrySetException( e );
                }
            }
        }
    }

    private async ValueTask AddOrUpdatePlayer( StationPlayerRequest request, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( request );

        if( contexts.TryGetValue( request.Channel.GuildId, out var context ) )
        {
            // if( context.ChannelId == request.Channel.Id && context.StationId == request.StationId )
            // {
            //     return;
            // }

            await context.DisposeAsync();
        }

        var player = await factory.Create(
            request.StationId,
            cancellation );

        var voice = await CreateClient(
            request.Channel.GuildId,
            request.Channel.Id,
            cancellation );

        contexts[ request.Channel.GuildId ] = new( player, voice );

        async Task<VoiceClient> CreateClient( ulong guildId, ulong channelId, CancellationToken cancellation )
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero( channelId );

            var client = await gateway.JoinVoiceChannelAsync(
                guildId,
                channelId,
                new()
                {
                    Logger = new ConsoleLogger()
                },
                cancellation );

            await client.StartAsync( cancellation );
            await client.EnterSpeakingStateAsync(
                new( SpeakingFlags.Microphone ),
                default,
                cancellation );

            return client;
        }
    }
}

internal sealed class StationPlayerContext : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task playback;
    private readonly StationPlayer player;
    private readonly VoiceClient voice;

    public ulong ChannelId => voice.ChannelId;
    public ulong GuildId => voice.GuildId;
    public Guid StationId => player.StationId;

    public StationPlayerContext( StationPlayer player, VoiceClient voice )
    {
        ArgumentNullException.ThrowIfNull( player );
        ArgumentNullException.ThrowIfNull( voice );

        this.player = player;
        this.voice = voice;

        playback = Task.Run( Play, cancellation.Token );
    }

    public async ValueTask DisposeAsync( )
    {
        await cancellation.CancelAsync();
        try
        {
            await playback.ConfigureAwait( false );
        }
        catch( OperationCanceledException )
        {
        }

        cancellation.Dispose();

        await voice.CloseAsync();
        voice.Dispose();

        await player.DisposeAsync();
    }

    private async Task Play( )
    {
        await using var output = new OpusEncodeStream(
            voice.CreateVoiceStream(),
            PcmFormat.Short,
            VoiceChannels.Stereo,
            OpusApplication.Audio );

        using( var ffmpeg = StartFfmpeg() )
        await using( var audio = await player.CreateAudioStream( cancellation.Token ) )
        {
            await Task.WhenAll(
                WriteAudio( ffmpeg, audio, cancellation.Token ),
                ReadAudio( ffmpeg, output, cancellation.Token ) );
        }

        await output.FlushAsync( cancellation.Token );

        static async Task ReadAudio( Process ffmpeg, Stream output, CancellationToken cancellation )
        {
            try
            {
                await ffmpeg.StandardOutput.BaseStream.CopyToAsync( output, cancellation );
            }
            catch( OperationCanceledException ) { }
            catch( IOException ) { } // destination closed
            finally
            {
                ffmpeg.Kill();
                ffmpeg.Dispose();
            }
        }

        static Process StartFfmpeg( ) => Process.Start( new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = string.Join( " ", [
                "-hide_banner",
                "-loglevel -8",
                "-reconnect 3",
                "-reconnect_streamed 3",
                "-reconnect_delay_max 5",
                "-i pipe:0",
                "-vn",
                "-f s16le",
                "-ar 48000",
                "-ac 2",
                "pipe:1" ] ),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        } ) ?? throw new InvalidOperationException( "Failed to start ffmpeg process." );

        static async Task WriteAudio( Process ffmpeg, Stream audio, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( ffmpeg );
            ArgumentNullException.ThrowIfNull( audio );

            try
            {
                await audio.CopyToAsync( ffmpeg.StandardInput.BaseStream, cancellation );
            }
            catch( OperationCanceledException ) { }
            catch( IOException ) { } // ffmpeg stdin closed (e.g. stream ended)
            finally
            {
                // NOTE: Signal EOF to ffmpeg — without this it will hang waiting for more input
                ffmpeg.StandardInput.Close();
            }
        }
    }
}

internal sealed record StationPlayerRequest( IVoiceGuildChannel Channel, Guid StationId )
{
    public TaskCompletionSource Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
}

internal static class StationPlayerRequestExtensions
{
    public static async Task Invoke( this Channel<StationPlayerRequest> queue, StationPlayerRequest request, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( queue );
        ArgumentNullException.ThrowIfNull( request );

        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync( request, cancellation );
            await request.Completion.Task.ConfigureAwait( false );
        }
    }
}