using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using RadioBrowser = Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Platform.Api.Infrastructure;

internal sealed class WadioApi(
    HybridCache cache,
    Octokit.IGitHubClient github,
    RadioBrowser.IRadioBrowserClient radioBrowser ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( cache, radioBrowser );
    public ILanguagesApi Languages { get; } = new LanguagesApi( cache, radioBrowser );
    public IReleasesApi Releases { get; } = new ReleasesApi( github );
    public IStationsApi Stations { get; } = new StationsApi( cache, radioBrowser );
    public ITagsApi Tags { get; } = new TagsApi( cache, radioBrowser );

    public ValueTask<WadioVersion> Version( CancellationToken cancellation = default ) => new( WadioVersion.Current );
}

sealed file class CountriesApi( HybridCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ICountriesApi
{
    public async IAsyncEnumerable<Country> Get( [EnumeratorCancellation] CancellationToken cancellation )
    {
        var countries = await cache.GetOrCreateAsync(
            WadioApiCacheKeys.Countries,
            radioBrowser,
            GetFromCache,
            new()
            {
                Expiration = TimeSpan.FromMinutes( 45 ),
                LocalCacheExpiration = TimeSpan.FromMinutes( 15 ),
            },
            default,
            cancellation ) ?? [];

        foreach( var country in countries )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return country;
        }

        static ValueTask<Country[]> GetFromCache( RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation ) => radioBrowser.GetCounties( new()
        {
            HideBroken = true,
            Order = RadioBrowser.CountryOrderBy.StationCount,
            Reverse = true,
        }, cancellation ).Select( country => new Country( country.Code, country.StationCount, country.Name ) ).ToArrayAsync( cancellation );
    }
}

sealed file class LanguagesApi( HybridCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ILanguagesApi
{
    public async IAsyncEnumerable<Language> Get( [EnumeratorCancellation] CancellationToken cancellation )
    {
        var languages = await cache.GetOrCreateAsync(
            WadioApiCacheKeys.Languages,
            radioBrowser,
            GetFromCache,
            new()
            {
                Expiration = TimeSpan.FromMinutes( 45 ),
                LocalCacheExpiration = TimeSpan.FromMinutes( 15 ),
            },
            default,
            cancellation ) ?? [];

        foreach( var language in languages )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return language;
        }

        static ValueTask<Language[]> GetFromCache( RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation ) => radioBrowser.GetLanguages( new()
        {
            HideBroken = true,
            Order = RadioBrowser.LanguageOrderBy.StationCount,
            Reverse = true,
        }, cancellation ).Select( country => new Language( country.Code, country.StationCount, country.Name ) ).ToArrayAsync( cancellation );
    }
}

sealed file class ReleasesApi( Octokit.IGitHubClient github ) : IReleasesApi
{
    public async IAsyncEnumerable<Release> Get( [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        var releases = await github.Repository.Release.GetAll( "ESCd", "Wadio" );
        foreach( var (release, index) in releases.Select( ( release, index ) => (release, index) ) )
        {
            cancellation.ThrowIfCancellationRequested();

            if( release.Draft || !release.PublishedAt.HasValue )
            {
                continue;
            }

            var version = WadioVersion.Parse( release.TagName.TrimStart( 'v' ) );
            // if( version > WadioVersion.Current )
            // {
            //     // NOTE: ignore pre-releases
            //     continue;
            // }

            yield return new(
                index is 0,
                release.Body,
                release.PublishedAt.Value,
                new( release.Url ),
                version );
        }
    }
}

sealed file class StationsApi( HybridCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : IStationsApi
{
    private static RadioBrowser.SearchParameters CreateSearch( Func<RadioBrowser.SearchParameters, RadioBrowser.SearchParameters> factory ) => factory( new()
    {
        HideBroken = true,
        IsHttps = true,
    } );

    public ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation )
    {
        return cache.GetOrCreateAsync(
            WadioApiCacheKeys.StationById( stationId ),
            new GetStationState( radioBrowser, stationId ),
            GetFromCache,
            new()
            {
                Expiration = TimeSpan.FromMinutes( 45 ),
                LocalCacheExpiration = TimeSpan.FromMinutes( 15 ),
            },
            default,
            cancellation );

        static async ValueTask<Station?> GetFromCache( GetStationState state, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( state );

            var station = await state.RadioBrowser.GetStation( state.StationId, cancellation );
            if( station is null )
            {
                return default;
            }

            return Map( station );
        }
    }

    public async Task<Station?> Random( SearchStationsParameters? parameters = default, CancellationToken cancellation = default )
    {
        var stations = await radioBrowser.Search( CreateSearch( search => WithParameters( search, parameters ) with
        {
            Limit = 250,
            Order = RadioBrowser.StationOrderBy.Random,
        } ), cancellation ).ToListAsync( cancellation );

        if( stations.Count is 0 )
        {
            return default;
        }

        return Map( stations[ System.Random.Shared.Next( stations.Count ) ] );
    }

    public IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( parameters );

        var search = radioBrowser.Search(
            CreateSearch( search => WithParameters( search, parameters ) ),
            cancellation );

        return search.Select( Map );
    }

    public async Task<bool> Track( Guid stationId, CancellationToken cancellation )
    {
        var click = await radioBrowser.Click( stationId, cancellation );
        return click?.Success is true;
    }

    public async Task<bool> Vote( Guid stationId, CancellationToken cancellation )
    {
        var vote = await radioBrowser.Vote( stationId, cancellation );
        if( vote?.Success is true )
        {
            await cache.RemoveAsync( WadioApiCacheKeys.StationById( stationId ), cancellation );
            return true;
        }

        return false;
    }

    private static Station Map( RadioBrowser.Station station ) => new( station.Id, station.Name.Trim(), station.ResolvedUrl ?? station.Url )
    {
        CheckedAt = station.LastCheckTime,
        Bitrate = station.Bitrate,
        Codec = CodecString.Parse( station.Codec ),
        Country = station.Country?.Trim(),
        CountryCode = station.CountryCode,
        HomepageUrl = station.HomepageUrl,
        IconUrl = station.IconUrl,
        IsHls = station.IsHls,
        Latitude = station.Latitude,
        Longitude = station.Longitude,
        Metrics = new( station.ClickCount, station.ClickTrend, station.Votes ),
        Languages = station.Languages ?? [],
        Tags = station.Tags ?? [],
        UpdatedAt = station.LastChangeTime,
    };

    private static RadioBrowser.SearchParameters WithParameters( RadioBrowser.SearchParameters search, SearchStationsParameters? parameters )
    {
        ArgumentNullException.ThrowIfNull( search );
        if( parameters is null )
        {
            return search;
        }

        return search with
        {
            Codec = parameters.Codec.HasValue ? CodecString.Format( parameters.Codec.Value ) : default,
            CountryCode = parameters.CountryCode,
            GeoDistance = parameters.Proximity?.Radius,
            GeoLatitude = parameters.Proximity?.Latitude,
            GeoLongitude = parameters.Proximity?.Longitude,

            // NOTE: force `HasGeoInfo` when filtering by `Proximity`
            HasGeoInfo = parameters.Proximity is not null ? true : parameters.HasLocation,
            Language = parameters.LanguageCode,
            Limit = parameters.Count,
            Name = parameters.Name,
            Offset = parameters.Offset,
            Order = parameters.Order switch
            {
                null or StationOrderBy.Name => RadioBrowser.StationOrderBy.Name,
                StationOrderBy.LastPlayed => RadioBrowser.StationOrderBy.ClickTimestamp,
                StationOrderBy.MostPlayed => RadioBrowser.StationOrderBy.ClickCount,
                StationOrderBy.Random => RadioBrowser.StationOrderBy.Random,
                StationOrderBy.RecentlyUpdated => RadioBrowser.StationOrderBy.ChangeTimestamp,
                StationOrderBy.Trending => RadioBrowser.StationOrderBy.ClickTrend,
                StationOrderBy.Votes => RadioBrowser.StationOrderBy.Votes,

                _ => throw new NotSupportedException()
            },
            Reverse = parameters.Reverse,
            Tag = parameters.Tag,
            Tags = parameters.Tags,
        };
    }

    private sealed record GetStationState( RadioBrowser.IRadioBrowserClient RadioBrowser, Guid StationId );
}

sealed file class TagsApi( HybridCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ITagsApi
{
    public async IAsyncEnumerable<Tag> Get( [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        var tags = await cache.GetOrCreateAsync(
            WadioApiCacheKeys.Tags,
            radioBrowser,
            GetFromCache,
            new()
            {
                Expiration = TimeSpan.FromMinutes( 45 ),
                LocalCacheExpiration = TimeSpan.FromMinutes( 15 ),
            },
            default,
            cancellation ) ?? [];

        foreach( var tag in tags )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return tag;
        }

        static ValueTask<Tag[]> GetFromCache( RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation ) => radioBrowser.GetTags( new()
        {
            HideBroken = true,
            Order = RadioBrowser.TagOrderBy.StationCount,
            Reverse = true,
        }, cancellation ).Select( tag => new Tag( tag.StationCount, tag.Name ) ).ToArrayAsync( cancellation );
    }
}

static file class WadioApiCacheKeys
{
    private const string Prefix = "WadioApi";

    public static readonly string Countries = $"${Prefix}/{nameof( Countries )}";
    public static readonly string Languages = $"${Prefix}/{nameof( Languages )}";
    public static string StationById( Guid stationId ) => $"${Prefix}/{nameof( StationById )}/{stationId}";
    public static readonly string Tags = $"${Prefix}/{nameof( Tags )}";
}

static file class CacheEntryExtensions
{
    public static TEntry WithWadioApiDefaults<TEntry>( this TEntry entry )
        where TEntry : class, ICacheEntry
    {
        ArgumentNullException.ThrowIfNull( entry );

        entry.SetAbsoluteExpiration( TimeSpan.FromMinutes( 45 ) )
            .SetSlidingExpiration( TimeSpan.FromMinutes( 5 ) );

        return entry;
    }
}