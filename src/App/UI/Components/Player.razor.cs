using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components;

public sealed record PlayerState : State
{
    public bool IsLoading { get; init; }
    public bool IsMuted { get; init; }
    public Station? Station { get; init; }
    public float Volume { get; init; } = .24f;

    internal static async ValueTask<PlayerState> Load( LocalStorageInterop storage, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( state );

        var data = await storage.Get<PlayerData>( "player" );
        if( data is not null )
        {
            return state with
            {
                IsMuted = data.IsMuted,
                Volume = data.Volume,
            };
        }

        return state;
    }

    internal static async IAsyncEnumerable<PlayerState> Play( PlayerAudio audio, Station station, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( station );
        ArgumentNullException.ThrowIfNull( state );

        if( state.Station?.Id == station.Id )
        {
            yield break;
        }

        yield return state = (state with
        {
            IsLoading = true,
            Station = default,
        });

        await audio.Play( station, state.AsAudioOptions() );
        yield return state with
        {
            IsLoading = false,
            Station = station,
        };
    }

    internal static async IAsyncEnumerable<PlayerState> Stop( PlayerAudio audio, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state with
        {
            IsLoading = false,
            Station = default,
        };

        await audio.Stop();
    }

    internal static async IAsyncEnumerable<PlayerState> ToggleMute( LocalStorageInterop storage, PlayerAudio audio, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsMuted = await audio.Muted( !state.IsMuted )
        });

        await StorePlayerData( storage, state );
    }

    internal static async IAsyncEnumerable<PlayerState> VolumeChanged( LocalStorageInterop storage, PlayerAudio audio, float volume, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        if( state.Volume == volume )
        {
            yield break;
        }

        if( volume <= 0 && !state.IsMuted )
        {
            state = state with
            {
                IsMuted = await audio.Muted( true ),
            };
        }
        else if( volume > 0 && state.IsMuted )
        {
            state = state with
            {
                IsMuted = await audio.Muted( false ),
            };
        }

        yield return state = (state with
        {
            Volume = await audio.Volume( volume ),
        });

        await StorePlayerData( storage, state );
    }

    private static ValueTask StorePlayerData( LocalStorageInterop storage, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( state );

        return storage.Set<PlayerData>( "player", new( state.IsMuted, state.Volume ) );
    }
}

public sealed class PlayerContext : IDisposable
{
    private readonly SemaphoreSlim locker = new( 1, 1 );
    private readonly List<Func<Station?, CancellationToken, ValueTask>> onChanging = [];
    private readonly List<Func<Station?, CancellationToken, ValueTask>> onChanged = [];

    public Station? Station { get; private set; }

    public void Dispose( )
    {
        locker.Dispose();
        onChanging.Clear();
        onChanged.Clear();
    }

    public IDisposable OnChanging( Func<Station?, CancellationToken, ValueTask> subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );

        onChanging.Add( subscriber );
        return new Subscription( this, subscriber );
    }

    public IDisposable OnChanged( Func<Station?, CancellationToken, ValueTask> subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );

        onChanged.Add( subscriber );
        return new Subscription( this, subscriber );
    }

    public async ValueTask Update( Station? station, CancellationToken cancellation = default )
    {
        if( Station?.Id == station?.Id )
        {
            return;
        }

        await locker.WaitAsync( cancellation );
        try
        {
            foreach( var subscriber in onChanging )
            {
                await subscriber( station, cancellation );
            }

            Station = station;

            foreach( var subscriber in onChanged )
            {
                await subscriber( station, cancellation );
            }
        }
        finally
        {
            locker.Release();
        }
    }

    private sealed class Subscription( PlayerContext context, Func<Station?, CancellationToken, ValueTask> subscriber ) : IDisposable
    {
        public void Dispose( ) => context.onChanging.Remove( subscriber );
    }
}

sealed file record PlayerData( bool IsMuted, float Volume );

internal static class PlayerStateExtensions
{
    public static PlayerAudioOptions AsAudioOptions( this PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return new( state.IsMuted, state.Volume );
    }
}