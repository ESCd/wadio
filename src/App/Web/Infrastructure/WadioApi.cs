using System.Runtime.CompilerServices;
using Wadio.App.UI.Abstractions;
using RadioBrowser = Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.App.Web.Infrastructure;

internal sealed class WadioApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : IWadioApi
{
    public IStationsApi Stations { get; } = new StationsApi( radioBrowser );
}

sealed file class StationsApi( RadioBrowser.IRadioBrowserClient radioBrowser ) : IStationsApi
{
    private static RadioBrowser.SearchParameters CreateSearch( Func<RadioBrowser.SearchParameters, RadioBrowser.SearchParameters> factory ) => factory( new()
    {
        HideBroken = true,
        IsHttps = true,
    } );

    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default )
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
            station = await Random( radioBrowser, cancellation );
        }

        if( station is not null )
        {
            return Map( station );
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

    public async IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation )
    {
        await foreach( var station in radioBrowser.Search( CreateSearch( search => search with
        {
            Codec = parameters.Codec?.ToString(),
            Limit = parameters.Count,
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
        } ), cancellation ) )
        {
            yield return Map( station );
        }
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
        Metrics = new( station.ClickCount, station.ClickTrend, station.Votes ),
        Languages = station.Languages ?? [],
        Tags = station.Tags ?? [],
        UpdatedAt = station.LastChangeTime,
    };
}