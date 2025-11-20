using System.Diagnostics.CodeAnalysis;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Components;
using Wadio.App.UI.Infrastructure;

namespace Wadio.App.UI.Pages;

public sealed record StationState : State<StationState>
{
    // NOTE: 50 miles (in meters)
    private const double NearbyRadius = 50 * 1609.344;
    public const int RelatedCount = 12;

    public bool HasVoted { get; init; }

    [MemberNotNullWhen( false, nameof( Station ) )]
    public bool IsLoading { get; init; } = true;
    public StationCarouselData? Nearby { get; init; }
    public StationCarouselData? Related { get; init; }
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

            Nearby = station?.Latitude.HasValue is true && station?.Longitude.HasValue is true ? new() : default,
            Related = station?.Tags.Length is not (null or 0) ? new() : default,
            Station = station,
        };
    }

    internal static async IAsyncEnumerable<StationState> LoadNearby( IStationsApi api, StationState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        if( state.Nearby is null || state.Station is null )
        {
            yield break;
        }

        var search = api.Search( new()
        {
            // NOTE: request one extra, so we can trim the current station if present
            Count = RelatedCount + 1,
            Order = StationOrderBy.Random,
            Proximity = new( state.Station.Latitude!.Value, state.Station.Longitude!.Value, NearbyRadius )
        } );

        yield return state with
        {
            Nearby = new()
            {
                IsLoading = false,

                // NOTE: filter out current station if present
                Value = await search.Where( station => station.Id != state.Station.Id )
                    .Take( RelatedCount )
                    .ToImmutableArrayAsync()
            }
        };
    }

    internal static async IAsyncEnumerable<StationState> LoadRelated( IStationsApi api, StationState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        if( state.Related is null || state.Station is null )
        {
            yield break;
        }

        var search = api.Search( new()
        {
            // NOTE: request one extra, so we can trim the current station if present
            Count = RelatedCount + 1,
            Order = StationOrderBy.Random,
            Tags = PickTags( state.Station )
        } );

        yield return state with
        {
            Related = new()
            {
                IsLoading = false,

                // NOTE: filter out current station if present
                Value = await search.Where( station => station.Id != state.Station.Id )
                    .Take( RelatedCount )
                    .ToImmutableArrayAsync()
            }
        };

        static string[] PickTags( Abstractions.Api.Station station )
        {
            ArgumentNullException.ThrowIfNull( station );
            if( station.Tags.Length <= 3 )
            {
                return station.Tags;
            }

            var tags = new List<string>( station.Tags );
            while( tags.Count > 3 )
            {
                // NOTE: reduce to 3 random tags
                tags.RemoveAt( Random.Shared.Next( 0, tags.Count ) );
            }

            return [ .. tags ];
        }
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