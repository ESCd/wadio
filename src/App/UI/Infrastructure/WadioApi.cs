using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Json;

namespace Wadio.App.UI.Infrastructure;

internal sealed class WadioApi( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IWadioApi
{
    public IStationsApi Stations { get; } = new StationsApi( http, queryStringPool );
}

sealed file class StationsApi( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IStationsApi
{
    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default ) => await http.GetFromJsonAsync( $"stations/{stationId}", AppJsonContext.Default.Station, cancellation );

    public async IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation )
    {
        var query = queryStringPool.Get()
            .Append( nameof( parameters.Codec ), ( int? )parameters.Codec )
            .Append( nameof( parameters.Count ), parameters.Count )
            .Append( nameof( parameters.Offset ), parameters.Offset )
            .Append( nameof( parameters.Order ), ( int )parameters.Order )
            .Append( nameof( parameters.Reverse ), parameters.Reverse );

        try
        {
            await foreach( var station in http.GetFromJsonAsAsyncEnumerable( $"stations{query}", AppJsonContext.Default.Station, cancellation ) )
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

    public Task<Station?> Random( CancellationToken cancellation = default ) => http.GetFromJsonAsync( "stations/random", AppJsonContext.Default.Station, cancellation );
}