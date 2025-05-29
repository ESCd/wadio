using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser;

internal sealed class RadioBrowserClient( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IRadioBrowserClient
{
    public async IAsyncEnumerable<Country> GetCounties( GetCountriesParameters parameters, [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( parameters );

        var query = queryStringPool.Get()
            .Append( "hidebroken", parameters.HideBroken )
            .Append( "limit", parameters.Limit )
            .Append( "offset", parameters.Offset )
            .Append( "order", parameters.Order?.ToString().ToLowerInvariant() )
            .Append( "reverse", parameters.Reverse );

        try
        {
            await foreach( var country in http.GetFromJsonAsAsyncEnumerable( $"countries{query}", RadioBrowserJsonContext.Default.Country, cancellation ) )
            {
                if( country is not null )
                {
                    yield return country;
                }
            }
        }
        finally
        {
            queryStringPool.Return( query );
        }
    }

    public async IAsyncEnumerable<Language> GetLanguages( GetLanguagesParameters parameters, [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( parameters );

        var query = queryStringPool.Get()
            .Append( "hidebroken", parameters.HideBroken )
            .Append( "limit", parameters.Limit )
            .Append( "offset", parameters.Offset )
            .Append( "order", parameters.Order?.ToString().ToLowerInvariant() )
            .Append( "reverse", parameters.Reverse );

        try
        {
            await foreach( var language in http.GetFromJsonAsAsyncEnumerable( $"languages{query}", RadioBrowserJsonContext.Default.Language, cancellation ) )
            {
                if( language is not null )
                {
                    yield return language;
                }
            }
        }
        finally
        {
            queryStringPool.Return( query );
        }
    }

    public ValueTask<Station?> GetStation( Guid stationId, CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( $"stations/byuuid/{stationId}", RadioBrowserJsonContext.Default.Station, cancellation )
            .FirstOrDefaultAsync( cancellation );

    public Task<ServiceStatistics?> GetStatistics( CancellationToken cancellation = default ) => http.GetFromJsonAsync( "stats", RadioBrowserJsonContext.Default.ServiceStatistics, cancellation );

    public async IAsyncEnumerable<Tag> GetTags( GetTagsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( parameters );

        var query = queryStringPool.Get()
            .Append( "hidebroken", parameters.HideBroken )
            .Append( "limit", parameters.Limit )
            .Append( "offset", parameters.Offset )
            .Append( "order", parameters.Order?.ToString().ToLowerInvariant() )
            .Append( "reverse", parameters.Reverse );

        try
        {
            await foreach( var tag in http.GetFromJsonAsAsyncEnumerable( $"tags{query}", RadioBrowserJsonContext.Default.Tag, cancellation ) )
            {
                if( tag is not null )
                {
                    yield return tag;
                }
            }
        }
        finally
        {
            queryStringPool.Return( query );
        }
    }

    public async IAsyncEnumerable<Station> Search( SearchParameters parameters, [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( parameters );

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
            .Append( "tag", parameters.Tag );

        if( parameters.Tags?.Length > 0 )
        {
            query = query.Append( "tagList", string.Join( ',', parameters.Tags.Select( tag => tag?.Trim() ).Where( tag => !string.IsNullOrEmpty( tag ) ) ) );
        }

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