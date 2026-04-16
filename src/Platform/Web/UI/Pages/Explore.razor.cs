using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using ESCd.AspNetCore.Components.Stateful;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Web.UI.Infrastructure;
using Wadio.Platform.Web.UI.Infrastructure.Imports;
using Wadio.Platform.Web.UI.Interop;

namespace Wadio.Platform.Web.UI.Pages;

public sealed record ExploreState : State<ExploreState>
{
    public Coordinate Center { get; init; } = (41.881832, -87.623177);
    public bool IsLoading { get; init; } = true;
    public bool IsReady { get; init; }
    public bool IsSearching { get; init; }
    public ProximitySearchParameter? Proximity { get; init; }
    public FrozenDictionary<Guid, Api.Abstractions.Station> Stations { get; init; } = FrozenDictionary<Guid, Api.Abstractions.Station>.Empty;

    internal static async ValueTask<ExploreState> Load( IStationsApi api, GeolocationInterop geolocation, ExploreState state, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( geolocation );
        ArgumentNullException.ThrowIfNull( state );

        var center = await GetCenter( api, geolocation, cancellation );
        if( center is not null )
        {
            return state with
            {
                Center = center,
                IsLoading = false,
            };
        }

        return state with
        {
            IsLoading = false
        };

        static async ValueTask<Coordinate?> GetCenter( IStationsApi api, GeolocationInterop geolocation, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentNullException.ThrowIfNull( geolocation );

            try
            {
                return await geolocation.GetCurrentPosition( new()
                {
                    EnableHighAccuracy = false,
                    MaximumAge = TimeSpan.FromMinutes( 5 ).TotalMilliseconds,
                    Timeout = TimeSpan.FromSeconds( 5 ).TotalMilliseconds
                }, cancellation );
            }
            catch( GeolocationException )
            {
            }

            var station = await api.Random( new()
            {
                Count = 1,
                HasLocation = true,
            }, cancellation );

            if( station?.TryGetLocation( out var location ) is true )
            {
                return location;
            }

            return default;
        }
    }

    internal static async IAsyncEnumerable<ExploreState> Search(
        IStationsApi api,
        IAsyncCache cache,
        ProximitySearchParameter proximity,
        ExploreState state,
        [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( cache );
        ArgumentNullException.ThrowIfNull( proximity );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsReady = true,
            IsSearching = true,
        });

        yield return state with
        {
            IsSearching = false,
            Proximity = proximity,
            Stations = await ExecuteSearch( api, cache, proximity, cancellation ),
        };

        static async ValueTask<FrozenDictionary<Guid, Api.Abstractions.Station>> ExecuteSearch(
            IStationsApi api,
            IAsyncCache cache,
            ProximitySearchParameter proximity,
            CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentNullException.ThrowIfNull( cache );
            ArgumentNullException.ThrowIfNull( proximity );

            return await cache.GetOrCreateAsync(
                ExploreCacheKeys.StationsByProximity( proximity ),
                ( entry, cancellation ) => GetFromCache( entry, api, proximity, cancellation ),
                cancellation ) ?? FrozenDictionary<Guid, Api.Abstractions.Station>.Empty;

            static async ValueTask<FrozenDictionary<Guid, Api.Abstractions.Station>> GetFromCache(
                ICacheEntry entry,
                IStationsApi api,
                ProximitySearchParameter proximity,
                CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( entry );
                ArgumentNullException.ThrowIfNull( api );
                ArgumentNullException.ThrowIfNull( proximity );

                entry.SetAbsoluteExpiration( TimeSpan.FromMinutes( 2.5 ) )
                    .SetSlidingExpiration( TimeSpan.FromSeconds( 45 ) );

                var search = api.Search( new()
                {
                    Count = default,
                    HasLocation = true,
                    Proximity = proximity,
                    Order = StationOrderBy.Random,
                }, cancellation );

                return await search.ToFrozenDictionaryAsync( station => station.Id, cancellation );
            }
        }
    }
}

static file class ExploreCacheKeys
{
    public static CacheKey StationsByProximity( ProximitySearchParameter proximity ) => new( nameof( ExploreState ), nameof( ExploreState.Stations ), proximity.ToString() );
}