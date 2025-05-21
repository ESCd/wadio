using System.Collections.Immutable;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Components;

namespace Wadio.App.UI.Pages;

public sealed record DiscoverState : State
{
    private const uint StationCount = 12;

    public StationData Popular { get; init; } = new();
    public StationData Random { get; init; } = new();
    public StationData RecentlyUpdated { get; init; } = new();
    public StationData Trending { get; init; } = new();

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

        await foreach( var mutation in RefreshRecentlyUpdated( api, state ) )
        {
            yield return state = mutation;
        }

        await foreach( var mutation in RefreshRandom( api, state ) )
        {
            yield return state = mutation;
        }
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

public sealed record StationData
{
    public bool IsLoading { get; init; } = true;
    public ImmutableArray<Abstractions.Station> Value { get; init; } = [];
}