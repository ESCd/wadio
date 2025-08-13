using System.Runtime.CompilerServices;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.App.UI.Abstractions;
using RadioBrowser = Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.App.Web.Infrastructure;

internal sealed class WadioApi( IAsyncCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( cache, radioBrowser );
    public ILanguagesApi Languages { get; } = new LanguagesApi( cache, radioBrowser );
    public IStationsApi Stations { get; } = new StationsApi( cache, radioBrowser );
    public ITagsApi Tags { get; } = new TagsApi( cache, radioBrowser );
}

sealed file class CountriesApi( IAsyncCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ICountriesApi
{
    public async IAsyncEnumerable<Country> Get( [EnumeratorCancellation] CancellationToken cancellation )
    {
        var countries = await cache.GetOrCreateAsync(
            WadioCacheKeys.Countries,
            ( entry, cancellation ) => GetFromCache( entry, radioBrowser, cancellation ),
            cancellation ) ?? [];

        foreach( var country in countries )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return country;
        }

        static async ValueTask<Country[]> GetFromCache( ICacheEntry entry, RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation )
        {
            entry.WithWadioApiDefaults();

            return await radioBrowser.GetCounties( new()
            {
                HideBroken = true,
                Order = RadioBrowser.CountryOrderBy.StationCount,
                Reverse = true,
            }, cancellation ).Select( country => new Country( country.Code, country.StationCount, country.Name ) ).ToArrayAsync( cancellation );
        }
    }
}

sealed file class LanguagesApi( IAsyncCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ILanguagesApi
{
    public async IAsyncEnumerable<Language> Get( [EnumeratorCancellation] CancellationToken cancellation )
    {
        var languages = await cache.GetOrCreateAsync(
            WadioCacheKeys.Languages,
            ( entry, cancellation ) => GetFromCache( entry, radioBrowser, cancellation ),
            cancellation ) ?? [];

        foreach( var language in languages )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return language;
        }

        static async ValueTask<Language[]> GetFromCache( ICacheEntry entry, RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation )
        {
            entry.WithWadioApiDefaults();

            return await radioBrowser.GetLanguages( new()
            {
                HideBroken = true,
                Order = RadioBrowser.LanguageOrderBy.StationCount,
                Reverse = true,
            }, cancellation ).Select( country => new Language( country.Code, country.StationCount, country.Name ) ).ToArrayAsync( cancellation );
        }
    }
}

sealed file class StationsApi( IAsyncCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : IStationsApi
{
    private static RadioBrowser.SearchParameters CreateSearch( Func<RadioBrowser.SearchParameters, RadioBrowser.SearchParameters> factory ) => factory( new()
    {
        HideBroken = true,
        IsHttps = true,
    } );

    public ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation )
    {
        return cache.GetOrCreateAsync(
            WadioCacheKeys.StationById( stationId ),
            ( entry, cancellation ) => GetFromCache( entry, radioBrowser, stationId, cancellation ),
            cancellation );

        static async ValueTask<Station?> GetFromCache( ICacheEntry entry, RadioBrowser.IRadioBrowserClient radioBrowser, Guid stationId, CancellationToken cancellation )
        {
            entry.WithWadioApiDefaults();

            var station = await radioBrowser.GetStation( stationId, cancellation );
            if( station is not null )
            {
                return Map( station );
            }

            return default;
        }
    }

    public async Task<Station?> Random( CancellationToken cancellation )
    {
        var retry = 0;
        RadioBrowser.Station? station = default;
        while( station is null && ++retry < 4 )
        {
            if( (station = await Random( radioBrowser, cancellation )) is not null )
            {
                return Map( station );
            }

            await Task.Yield();
        }

        return default;

        static async Task<RadioBrowser.Station?> Random( RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation )
        {
            var statistics = await radioBrowser.GetStatistics( cancellation );
            if( statistics is null )
            {
                return default;
            }

            return await radioBrowser.Search( CreateSearch( search => search with
            {
                Limit = 1,
                Offset = ( uint )System.Random.Shared.Next( 0, ( int )(statistics.Stations - statistics.BrokenStations) ),
                Order = RadioBrowser.StationOrderBy.Random,
            } ), cancellation ).FirstOrDefaultAsync( cancellation );
        }
    }

    public IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, CancellationToken cancellation )
        => radioBrowser.Search( CreateSearch( search => search with
        {
            Codec = parameters.Codec?.ToString(),
            CountryCode = parameters.CountryCode,
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
        } ), cancellation ).Select( Map );

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
            cache.Remove( WadioCacheKeys.StationById( stationId ) );
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
}

sealed file class TagsApi( IAsyncCache cache, RadioBrowser.IRadioBrowserClient radioBrowser ) : ITagsApi
{
    public async IAsyncEnumerable<Tag> Get( [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        var tags = await cache.GetOrCreateAsync(
            WadioCacheKeys.Tags,
            ( entry, cancellation ) => GetFromCache( entry, radioBrowser, cancellation ),
            cancellation ) ?? [];

        foreach( var tag in tags )
        {
            cancellation.ThrowIfCancellationRequested();
            yield return tag;
        }

        static async ValueTask<Tag[]> GetFromCache( ICacheEntry entry, RadioBrowser.IRadioBrowserClient radioBrowser, CancellationToken cancellation )
        {
            entry.WithWadioApiDefaults();

            return await radioBrowser.GetTags( new()
            {
                HideBroken = true,
                Order = RadioBrowser.TagOrderBy.StationCount,
                Reverse = true,
            }, cancellation ).Select( tag => new Tag( tag.StationCount, tag.Name ) ).ToArrayAsync( cancellation );
        }
    }
}

static file class WadioCacheKeys
{
    private const string Prefix = "Wadio";

    public static readonly CacheKey Countries = new( Prefix, nameof( Countries ) );
    public static readonly CacheKey Languages = new( Prefix, nameof( Languages ) );
    public static CacheKey StationById( Guid stationId ) => new( Prefix, nameof( StationById ), stationId.ToString() );
    public static readonly CacheKey Tags = new( Prefix, nameof( Tags ) );
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