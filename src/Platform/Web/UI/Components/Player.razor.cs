using System.Runtime.CompilerServices;
using ESCd.AspNetCore.Components.Stateful;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Web.UI.Interop;

namespace Wadio.Platform.Web.UI.Components;

public sealed record PlayerState : State<PlayerState>
{
    public bool IsLoading { get; init; }
    public bool IsMuted { get; init; }
    public MediaMetadata? Metadata { get; init; }
    public Station? Station { get; init; }
    public float Volume { get; init; } = .64f;

    internal static async ValueTask<PlayerState> Load( LocalStorageInterop storage, PlayerState state, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( state );

        var data = await storage.Get<PlayerData>( "player", cancellation );
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

    internal static async IAsyncEnumerable<PlayerState> Play( IStationsApi api, StationPlayer audio, Station station, PlayerState state, [EnumeratorCancellation] CancellationToken cancellation )
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

        await audio.Play(
            station,
            state.AsPlayerOptions(),
            cancellation );

        yield return state with
        {
            IsLoading = false,
            Station = station,
        };

        try
        {
            await api.Track( station.Id, cancellation );
        }
        catch( Exception e ) when( e is ApiProblemException or HttpRequestException or TaskCanceledException )
        {
            // NOTE: ignore errors
        }
    }

    internal static async IAsyncEnumerable<PlayerState> Stop( StationPlayer audio, PlayerState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state with
        {
            IsLoading = true,
        };

        await audio.Stop( cancellation );
        yield return state with
        {
            IsLoading = false,
            Station = default,
            Metadata = default,
        };
    }

    internal static async IAsyncEnumerable<PlayerState> ToggleMute( LocalStorageInterop storage, StationPlayer audio, PlayerState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( audio );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsMuted = await audio.Muted( !state.IsMuted, cancellation )
        });

        await StorePlayerData( storage, state, cancellation );
    }

    internal static async IAsyncEnumerable<PlayerState> VolumeChanged( LocalStorageInterop storage, StationPlayer audio, float volume, PlayerState state, [EnumeratorCancellation] CancellationToken cancellation )
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
            state = (state with
            {
                IsMuted = await audio.Muted( true, cancellation ),
            });
        }
        else if( volume > 0 && state.IsMuted )
        {
            state = (state with
            {
                IsMuted = await audio.Muted( false, cancellation ),
            });
        }

        yield return state = (state with
        {
            Volume = await audio.Volume( volume, cancellation ),
        });

        await StorePlayerData( storage, state, cancellation );
    }

    private static ValueTask StorePlayerData( LocalStorageInterop storage, PlayerState state, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( storage );
        ArgumentNullException.ThrowIfNull( state );

        return storage.Set<PlayerData>(
            "player",
            new( state.IsMuted, state.Volume ),
            cancellation );
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
