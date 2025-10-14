using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components;

public sealed record PlayerState : State<PlayerState>
{
    public bool IsLoading { get; init; }
    public bool IsMuted { get; init; }
    public MediaMetadata? Metadata { get; init; }
    public Station? Station { get; init; }
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

    internal static PlayerState MetaChanged( PlayerState state, MediaMetadata? meta )
    {
        ArgumentNullException.ThrowIfNull( state );

        if( state.Metadata == meta )
        {
            return state;
        }

        return state with
        {
            Metadata = meta,
        };
    }

    internal static async IAsyncEnumerable<PlayerState> Play( IStationsApi api, StationPlayer audio, Station station, PlayerState state )
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
            Metadata = default,
        });

        await audio.Play( station, state.AsPlayerOptions() );
        yield return state with
        {
            IsLoading = false,
            Station = station,
        };

        try
        {
            await api.Track( station.Id );
        }
        catch( Exception e ) when( e is ApiProblemException or HttpRequestException or TaskCanceledException )
        {
            // NOTE: ignore errors
        }
    }

    internal static async IAsyncEnumerable<PlayerState> Stop( StationPlayer audio, PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state with
        {
            IsLoading = false,
            Station = default,
            Metadata = default,
        };

        await audio.Stop();
    }

    internal static async IAsyncEnumerable<PlayerState> ToggleMute( LocalStorageInterop storage, StationPlayer audio, PlayerState state )
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

    internal static async IAsyncEnumerable<PlayerState> VolumeChanged( LocalStorageInterop storage, StationPlayer audio, float volume, PlayerState state )
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
    public static StationPlayerOptions AsPlayerOptions( this PlayerState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return new( state.IsMuted, state.Volume );
    }
}
