using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Components;
using Wadio.App.UI.Infrastructure;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Pages;

public sealed record ExploreState : State<ExploreState>
{
    public Coordinate Center { get; init; } = (41.881832, -87.623177);
    public bool IsLoading { get; init; } = true;
    public bool IsReady => Proximity is not null;
    public bool IsSearching { get; init; }
    public ProximitySearchParameter? Proximity { get; init; }
    public ImmutableDictionary<Guid, Abstractions.Api.Station> Stations { get; init; } = ImmutableDictionary<Guid, Abstractions.Api.Station>.Empty;

    internal static async ValueTask<ExploreState> Load( IStationsApi api, GeolocationInterop geolocation, ExploreState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( geolocation );
        ArgumentNullException.ThrowIfNull( state );

        var center = await GetCenter( api, geolocation );
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

        static async ValueTask<Coordinate?> GetCenter( IStationsApi api, GeolocationInterop geolocation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentNullException.ThrowIfNull( geolocation );

            try
            {
                return await geolocation.GetCurrentPosition();
            }
            catch( GeolocationException )
            {
            }

            var station = await api.Random( new()
            {
                Count = 1,
                HasLocation = true,
            } );

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
            IsSearching = true,
            Proximity = proximity,
        });

        yield return state with
        {
            IsSearching = false,
            Stations = await ExecuteSearch( api, cache, proximity, cancellation ),
        };

        static async ValueTask<ImmutableDictionary<Guid, Abstractions.Api.Station>> ExecuteSearch(
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
                cancellation ) ?? ImmutableDictionary<Guid, Abstractions.Api.Station>.Empty;

            static async ValueTask<ImmutableDictionary<Guid, Abstractions.Api.Station>> GetFromCache(
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

                return await search.ToImmutableDictionaryAsync( station => station.Id, cancellation );
            }
        }
    }
}

static file class ExploreCacheKeys
{
    public static CacheKey StationsByProximity( ProximitySearchParameter proximity ) => new( nameof( ExploreState ), nameof( ExploreState.Stations ), proximity.ToString() );
}