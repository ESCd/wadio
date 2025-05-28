using Wadio.App.UI.Abstractions;
using RadioBrowser = Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.App.Web.Infrastructure;

internal sealed class WadioApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( radioBrowser );
    public ILanguagesApi Languages { get; } = new LanguagesApi( radioBrowser );
    public IStationsApi Stations { get; } = new StationsApi( radioBrowser );
}

sealed file class CountriesApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : ICountriesApi
{
    public IAsyncEnumerable<Country> Get( CancellationToken cancellation )
        => radioBrowser.GetCounties( new()
        {
            HideBroken = true,
            Order = RadioBrowser.CountryOrderBy.StationCount,
            Reverse = true,
        }, cancellation ).Select( country => new Country( country.Code, country.Name, country.StationCount ) );
}

sealed file class LanguagesApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : ILanguagesApi
{
    public IAsyncEnumerable<Language> Get( CancellationToken cancellation )
        => radioBrowser.GetLanguages( new()
        {
            HideBroken = true,
            Order = RadioBrowser.LanguageOrderBy.StationCount,
            Reverse = true,
        }, cancellation ).Select( country => new Language( country.Code, country.Name, country.StationCount ) );
}

sealed file class StationsApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : IStationsApi
{
    private static RadioBrowser.SearchParameters CreateSearch( Func<RadioBrowser.SearchParameters, RadioBrowser.SearchParameters> factory ) => factory( new()
    {
        HideBroken = true,
        IsHttps = true,
    } );

    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation )
    {
        var station = await radioBrowser.GetStation( stationId, cancellation );
        if( station is not null )
        {
            return Map( station );
        }

        return default;
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

            await Task.Delay( 250, cancellation );
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
                StationOrderBy.Name => RadioBrowser.StationOrderBy.Name,
                StationOrderBy.LastViewed => RadioBrowser.StationOrderBy.ClickTimestamp,
                StationOrderBy.MostViewed => RadioBrowser.StationOrderBy.ClickCount,
                StationOrderBy.Random => RadioBrowser.StationOrderBy.Random,
                StationOrderBy.RecentlyUpdated => RadioBrowser.StationOrderBy.ChangeTimestamp,
                StationOrderBy.Trending => RadioBrowser.StationOrderBy.ClickTrend,
                StationOrderBy.Votes => RadioBrowser.StationOrderBy.Votes,

                _ => throw new NotSupportedException()
            },
            Reverse = parameters.Reverse,
            Tags = parameters.Tags,
        } ), cancellation ).Select( Map );

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