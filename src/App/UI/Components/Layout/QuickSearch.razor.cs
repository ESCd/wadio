using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Components.Layout;

public sealed record QuickSearchState : State<QuickSearchState>
{
    private const uint StationCount = 5;

    public ImmutableArray<Station>? Stations { get; init; }
    public bool IsLoading { get; init; }

    internal static QuickSearchState Reset( QuickSearchState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return state with
        {
            IsLoading = false,
            Stations = default,
        };
    }

    internal static async IAsyncEnumerable<QuickSearchState> Search( IStationsApi api, IAsyncCache cache, string query, QuickSearchState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( cache );
        ArgumentException.ThrowIfNullOrWhiteSpace( query );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsLoading = true,
            Stations = [],
        });

        yield return state with
        {
            IsLoading = false,
            Stations = await Search( api, cache, query, cancellation ),
        };

        static ValueTask<ImmutableArray<Station>> Search(
            IStationsApi api,
            IAsyncCache cache,
            string query,
            CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentNullException.ThrowIfNull( cache );
            ArgumentException.ThrowIfNullOrWhiteSpace( query );

            return cache.GetOrCreateAsync(
                QuickSearchCacheKeys.Search( query ),
                ( entry, token ) => Search( entry, api, query, token ),
                cancellation );

            static async ValueTask<ImmutableArray<Station>> Search(
                ICacheEntry entry,
                IStationsApi api,
                string query,
                CancellationToken cancellation )
            {
                entry.SetAbsoluteExpiration( TimeSpan.FromMinutes( 5 ) )
                    .SetSlidingExpiration( TimeSpan.FromMinutes( 1.75 ) );

                return [ .. await api.Search( new()
                {
                    Count = StationCount,
                    Name = query,
                    Order = StationOrderBy.Name,
                }, cancellation ).ToListAsync( cancellation ) ];
            }
        }
    }
}

static file class QuickSearchCacheKeys
{
    public static CacheKey Search( string query ) => new( nameof( QuickSearchState ), "search", query );
}