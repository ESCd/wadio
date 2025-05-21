using System.Diagnostics.CodeAnalysis;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Components;

namespace Wadio.App.UI.Pages;

public sealed record StationState : State
{
    [MemberNotNullWhen( false, nameof( Station ) )]
    public bool IsLoading { get; init; } = true;

    public Abstractions.Station? Station { get; init; }

    internal static async IAsyncEnumerable<StationState> Load( IStationsApi api, Guid stationId, StationState state )
    {
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
}