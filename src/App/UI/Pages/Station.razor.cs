using System.Diagnostics.CodeAnalysis;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Components;

namespace Wadio.App.UI.Pages;

public sealed record StationState : State<StationState>
{
    public bool HasVoted { get; init; }

    [MemberNotNullWhen( false, nameof( Station ) )]
    public bool IsLoading { get; init; } = true;
    public Abstractions.Api.Station? Station { get; init; }

    internal static async IAsyncEnumerable<StationState> Load( IStationsApi api, Guid stationId, StationState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsLoading = true,
        });

        var station = await api.Get( stationId );
        yield return state with
        {
            // NOTE: keep loading until client restores from persistence
            IsLoading = !OperatingSystem.IsBrowser(),

            Station = station,
        };
    }

    internal static StationState Restored( StationState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return state with
        {
            IsLoading = false,
        };
    }

    internal static async Task<StationState> Vote( IStationsApi api, StationState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        if( state.Station is not null && await api.Vote( state.Station!.Id ) )
        {
            return state with
            {
                HasVoted = true,
                Station = state.Station with
                {
                    Metrics = state.Station.Metrics with
                    {
                        Votes = state.Station.Metrics.Votes + 1,
                    }
                }
            };
        }

        return state;
    }
}