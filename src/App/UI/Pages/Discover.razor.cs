using System.Collections.Immutable;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Components;

namespace Wadio.App.UI.Pages;

public sealed record DiscoverState : State<DiscoverState>
{
    public const int StationCount = 12;

    public StationData Popular { get; init; } = new( StationOrderBy.MostViewed );
    public StationData Random { get; init; } = new( StationOrderBy.Random );
    public StationData RecentlyUpdated { get; init; } = new( StationOrderBy.RecentlyUpdated );
    public StationData Trending { get; init; } = new( StationOrderBy.Trending );

    internal static async IAsyncEnumerable<DiscoverState> Load( IStationsApi api, DiscoverState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        var parameters = new SearchStationsParameters
        {
            Count = StationCount,
        };

        yield return state = (state with
        {
            Trending = state.Trending with
            {
                IsLoading = false,
                Value = await Search( api, parameters with
                {
                    Order = StationOrderBy.Trending,
                    Reverse = true,
                } ),
            }
        });

        yield return state = (state with
        {
            Popular = state.Popular with
            {
                IsLoading = false,
                Value = await Search( api, parameters with
                {
                    Order = StationOrderBy.MostViewed,
                    Reverse = true,
                } ),
            }
        });

        yield return state = (state with
        {
            RecentlyUpdated = state.RecentlyUpdated with
            {
                IsLoading = false,
                Value = await Search( api, parameters with
                {
                    Order = StationOrderBy.RecentlyUpdated,
                    Reverse = true,
                } ),
            }
        });

        yield return state with
        {
            Random = state.Random with
            {
                IsLoading = false,
                Value = await Search( api, parameters with
                {
                    Order = StationOrderBy.Random,
                    Reverse = true,
                } ),
            }
        };
    }

    internal static async IAsyncEnumerable<DiscoverState> RefreshRandom( IStationsApi api, DiscoverState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            Random = state.Random with
            {
                IsLoading = true,
                Value = [],
            }
        });

        yield return state with
        {
            Random = state.Random with
            {
                IsLoading = false,
                Value = await Search( api, new()
                {
                    Count = StationCount,
                    Order = StationOrderBy.Random
                } ),
            }
        };
    }

    internal static async IAsyncEnumerable<DiscoverState> RefreshRecentlyUpdated( IStationsApi api, DiscoverState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            RecentlyUpdated = state.RecentlyUpdated with
            {
                IsLoading = true,
                Value = [],
            }
        });

        yield return state with
        {
            RecentlyUpdated = state.RecentlyUpdated with
            {
                IsLoading = false,
                Value = await Search( api, new()
                {
                    Count = StationCount,
                    Order = StationOrderBy.RecentlyUpdated,
                    Reverse = true,
                } ),
            }
        };
    }

    private static async Task<ImmutableArray<Abstractions.Station>> Search( IStationsApi api, SearchStationsParameters parameters )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );

        return [ .. await api.Search( parameters ).ToListAsync() ];
    }
}

public sealed record StationData( StationOrderBy Order )
{
    public bool IsLoading { get; init; } = true;
    public ImmutableArray<Abstractions.Station> Value { get; init; } = [];
}