using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser;

internal sealed class RadioBrowserClient( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IRadioBrowserClient
{
    public ValueTask<Station?> GetStation( Guid stationId, CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( $"stations/byuuid/{stationId}", RadioBrowserJsonContext.Default.Station, cancellation )
            .FirstOrDefaultAsync( cancellation );

    public Task<ServiceStatistics?> GetStatistics( CancellationToken cancellation = default ) => http.GetFromJsonAsync( "stats", RadioBrowserJsonContext.Default.ServiceStatistics, cancellation );

    public async IAsyncEnumerable<Station> Search( SearchParameters parameters, [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        var query = queryStringPool.Get()
            .Append( "countrycode", parameters.CountryCode )
            .Append( "hidebroken", parameters.HideBroken )
            .Append( "is_https", parameters.IsHttps )
            .Append( "language", parameters.Language )
            .Append( "limit", parameters.Limit )
            .Append( "name", parameters.Name )
            .Append( "offset", parameters.Offset )
            .Append( "order", parameters.Order?.ToString().ToLowerInvariant() )
            .Append( "reverse", parameters.Reverse )
            .Append( "state", parameters.State )
            .Append( "tagList", parameters.Tags ?? [] );

        try
        {
            await foreach( var station in http.GetFromJsonAsAsyncEnumerable( $"stations/search{query}", RadioBrowserJsonContext.Default.Station, cancellation ) )
            {
                if( station is not null )
                {
                    yield return station;
                }
            }
        }
        finally
        {
            queryStringPool.Return( query );
        }
    }
}