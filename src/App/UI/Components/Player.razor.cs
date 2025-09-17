using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components;

public sealed record PlayerState : State<PlayerState>
{
    public bool IsLoading { get; init; }
    public bool IsMuted { get; init; }
    public Station? Station { get; init; }
    public string? Title { get; init; }
    public float Volume { get; init; } = .64f;

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

    internal static async IAsyncEnumerable<PlayerState> Play( IStationsApi api, PlayerAudio audio, Station station, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( api );
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
            Title = default,
        });

        await audio.Play( station, state.AsAudioOptions() );
        yield return state with
        {
            IsLoading = false,
            Station = station,
        };

        try
        {
            await api.Track( station.Id );
        }
        catch( Exception e ) when( e is ApiProblemException or HttpRequestException )
        {
            // NOTE: ignore errors
        }
    }

    internal static async IAsyncEnumerable<PlayerState> Stop( PlayerAudio audio, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state with
        {
            IsLoading = false,
            Station = default,
            Title = default,
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

    internal static PlayerState TitleChanged( PlayerState state, string? title )
    {
        ArgumentNullException.ThrowIfNull( state );

        if( state.Title == title )
        {
            return state;
        }

        return state with
        {
            Title = title,
        };
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

sealed file record PlayerData( bool IsMuted, float Volume );
internal static class PlayerStateExtensions
{
    public static PlayerAudioOptions AsAudioOptions( this PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return new( state.IsMuted, state.Volume );
    }
}
