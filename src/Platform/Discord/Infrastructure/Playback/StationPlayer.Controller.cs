using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Rest;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed partial class StationPlayerController : IAsyncDisposable
{
    private const int MaxOutputs = 3;

    private CancellationTokenSource cancellation = new();
    private bool disposed;
    private Task? playback;
    private StationPlayer? player;
    private StationPlayerStatus? status;

    private readonly PcmEncoderPool encoders;
    private readonly GatewayClient gateway;
    private readonly ILogger<StationPlayerController> logger;
    private readonly List<RestMessage> outputs = new( MaxOutputs );
    private readonly StationPlayerStore store;
    private readonly StationPlayerRenderer renderer;
    private readonly VoiceClient voice;

    public ulong ChannelId => voice.ChannelId;
    public ulong GuildId => voice.GuildId;
    public Guid? StationId => player?.Station.Id;

    public StationPlayerController(
        PcmEncoderPool encoders,
        GatewayClient gateway,
        ILogger<StationPlayerController> logger,
        StationPlayerRenderer renderer,
        StationPlayerStore store,
        VoiceClient voice )
    {
        this.encoders = encoders;
        this.gateway = gateway;
        this.logger = logger;
        this.renderer = renderer;
        this.store = store;
        this.voice = voice;

        voice.Disconnect += OnDisconnect;
        voice.UserDisconnect += OnUserDisconnect;
    }

    public void AddOutput( RestMessage message )
    {
        ArgumentNullException.ThrowIfNull( message );

        if( outputs.Count is MaxOutputs )
        {
            outputs.RemoveAt( 0 );
        }

        outputs.Remove( message );
        outputs.Add( message );
    }

    private async ValueTask Cancel( bool reset = false )
    {
        if( playback is null )
        {
            return;
        }

        await cancellation.CancelAsync();
        try
        {
            await playback.ConfigureAwait( false );
        }
        catch( OperationCanceledException )
        {
        }

        playback = default;
        if( reset && !cancellation.TryReset() )
        {
            cancellation.Dispose();
            cancellation = new();
        }
    }

    private async ValueTask DestroyPlayer( )
    {
        if( player is not null )
        {
            player.Ended -= OnEnded;
            player.MetadataUpdated -= OnMetadataUpdated;

            await player.DisposeAsync();
            player = default;
        }
    }

    public async ValueTask DisposeAsync( )
    {
        disposed = true;

        await Cancel();
        cancellation.Dispose();

        await voice.CloseAsync();
        voice.Dispose();

        await DestroyPlayer();
        status = default;

        await Render();
        outputs.Clear();

        await gateway.UpdateVoiceStateAsync( new( voice.GuildId, default ) );
    }

    private async ValueTask OnDisconnect( DisconnectEventArgs e )
    {
        ArgumentNullException.ThrowIfNull( e );

        if( e.Reconnect )
        {
            return;
        }

        logger.OnDisconnected();
        if( await store.TryRemoveAsync( voice.GuildId, this ) is not true )
        {
            status = default;
            await DisposeAsync();
        }
    }

    private ValueTask OnEnded( Exception? e )
    {
        if( status is not null )
        {
            status = status with
            {
                Meta = default,
                RefreshedAt = DateTimeOffset.UtcNow,
            };
        }

        return Render();
    }

    private ValueTask OnMetadataUpdated( IReadOnlyDictionary<string, string> metadata )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        if( !metadata.Equals( status?.Meta ) )
        {
            status = (status ?? new( DateTimeOffset.UtcNow, player!.Station )) with
            {
                Meta = metadata,
                RefreshedAt = DateTimeOffset.UtcNow,
            };

            return Render();
        }

        return default;
    }

    private void OnPlayerChanging( StationPlayer player )
    {
        ArgumentNullException.ThrowIfNull( player );

        status = (status ?? new( DateTimeOffset.UtcNow, player.Station )) with
        {
            Meta = default,
            RefreshedAt = DateTimeOffset.UtcNow,
            Station = player.Station,
        };

        player.Ended += OnEnded;
        player.MetadataUpdated += OnMetadataUpdated;

        logger.OnPlayerChanged( status );
    }

    private async ValueTask OnUserDisconnect( UserDisconnectEventArgs e )
    {
        ArgumentNullException.ThrowIfNull( e );

        // NOTE: if the only user in the cache is the one that disconnected, then the bot is alone
        if( voice.Cache.Users.Count is 0 || (voice.Cache.Users.Count is 1 && voice.Cache.Users.Contains( e.UserId )) )
        {
            var timeout = TimeSpan.FromSeconds( 45 );
            await Task.Delay( timeout, cancellation.Token );

            // NOTE: if there are users in the cache, the bot is no longer alone
            if( disposed || voice.Cache.Users.Count is not 0 )
            {
                return;
            }

            logger.OnAbandoned( timeout );
            if( await store.TryRemoveAsync( voice.GuildId, this ) is not true )
            {
                await DisposeAsync();
            }
        }
    }

    public async ValueTask<StationPlayerStatus?> Play( StationPlayer player, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( player );
        ObjectDisposedException.ThrowIf( disposed, this );

        if( this.player == player )
        {
            return this.status;
        }

        await Cancel( true );
        await DestroyPlayer();

        this.player = player;
        OnPlayerChanging( player );

        playback = Task.Run( Play, cancellation );

        using( var timeout = new CancellationTokenSource( TimeSpan.FromSeconds( 7.5 ) ) )
        using( var combined = CancellationTokenSource.CreateLinkedTokenSource( timeout.Token, cancellation ) )
        {
            try
            {
                _ = await player.WaitUntilMetadata( combined.Token );
            }
            catch( OperationCanceledException e ) when( e.CancellationToken == combined.Token && timeout.IsCancellationRequested )
            {
            }
        }

        var status = await Status( cancellation );

        logger.OnPlayerChanged( status );
        return status;
    }

    private async Task Play( )
    {
        ObjectDisposedException.ThrowIf( disposed, this );
        ArgumentNullException.ThrowIfNull( player );

        var encoder = encoders.Get( player.Codec );
        try
        {
            await using var output = new OpusEncodeStream(
                voice.CreateVoiceStream(),
                PcmFormat.Short,
                VoiceChannels.Stereo,
                OpusApplication.Audio );

            await using var audio = await player.CreateAudioStream( cancellation.Token );
            await encoder.Encode(
                audio,
                output,
                cancellation.Token );

            await output.FlushAsync( cancellation.Token );
        }
        catch( Exception e )
        {
            logger.OnPlaybackError( e );
            throw;
        }
        finally
        {
            await encoders.Return( encoder );
        }
    }

    private async ValueTask Render( )
    {
        var result = await renderer.Render( outputs, status );
        if( result.Stale.Count is not 0 )
        {
            foreach( var message in result.Stale )
            {
                outputs.Remove( message );
            }

            outputs.TrimExcess();
        }
    }

    public ValueTask<StationPlayerStatus?> Status( CancellationToken _ )
    {
        ObjectDisposedException.ThrowIf( disposed, this );

        return new( status );
    }
}

internal static partial class StationPlayerControllerLogging
{
    [LoggerMessage( Level = LogLevel.Information, Message = "Client was disconnected after being abandoned for {timeout}." )]
    public static partial void OnAbandoned( this ILogger<StationPlayerController> logger, TimeSpan timeout );

    [LoggerMessage( Level = LogLevel.Debug, Message = "Client was disconnected." )]
    public static partial void OnDisconnected( this ILogger<StationPlayerController> logger );

    [LoggerMessage( Level = LogLevel.Error, Message = "Player encountered an error!" )]
    public static partial void OnPlaybackError( this ILogger<StationPlayerController> logger, Exception e );

    [LoggerMessage( Level = LogLevel.Debug, Message = "Player was changed: {status}" )]
    public static partial void OnPlayerChanged( this ILogger<StationPlayerController> logger, StationPlayerStatus? status );
}

internal sealed record StationPlayerStatus( DateTimeOffset StartedAt, Station Station )
{
    public IReadOnlyDictionary<string, string>? Meta { get; init; }
    public DateTimeOffset? RefreshedAt { get; init; }
}

internal delegate Task<RestMessage> StationPlayerBindingFactory( StationPlayerStatus? status );