using System.Collections.Immutable;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Components;

namespace Wadio.App.UI.Pages;

public sealed record DiscoverState : State<DiscoverState>
{
    public const int StationCount = 12;

    public StationCarouselData Popular { get; init; } = new();
    public StationCarouselData Random { get; init; } = new();
    public StationCarouselData RecentlyUpdated { get; init; } = new();
    public StationCarouselData Trending { get; init; } = new();

    internal static async IAsyncEnumerable<DiscoverState> Load( IStationsApi api, IAsyncCache cache, DiscoverState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( cache );
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
                Value = await cache.GetOrCreateAsync( DiscoverCacheKeys.Trending, ( entry, cancellation ) =>
                {
                    entry.SetDefaultExpiration();
                    return Search( api, parameters with
                    {
                        Order = StationOrderBy.Trending,
                        Reverse = true,
                    }, cancellation );
                } ),
            }
        });

        yield return state = (state with
        {
            Popular = state.Popular with
            {
                IsLoading = false,
                Value = await cache.GetOrCreateAsync( DiscoverCacheKeys.Popular, ( entry, cancellation ) =>
                {
                    entry.SetDefaultExpiration();
                    return Search( api, parameters with
                    {
                        Order = StationOrderBy.MostPlayed,
                        Reverse = true,
                    }, cancellation );
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

    private static async ValueTask<ImmutableArray<Abstractions.Api.Station>> Search( IStationsApi api, SearchStationsParameters parameters, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( parameters );

        return [
            .. await api.Search( parameters, cancellation )
                .ToListAsync( cancellation ) ];
    }
}

static file class DiscoverCacheKeys
{
    public static readonly CacheKey Popular = new( nameof( DiscoverState ), nameof( Popular ) );
    public static readonly CacheKey Trending = new( nameof( DiscoverState ), nameof( Trending ) );

    public static TEntry SetDefaultExpiration<TEntry>( this TEntry entry )
        where TEntry : ICacheEntry
    {
        entry.SetAbsoluteExpiration( TimeSpan.FromMinutes( 10 ) )
            .SetSlidingExpiration( TimeSpan.FromMinutes( 2.5 ) );

        return entry;
    }
}