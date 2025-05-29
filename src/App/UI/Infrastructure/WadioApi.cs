using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Json;

namespace Wadio.App.UI.Infrastructure;

internal sealed class WadioApi( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( http );
    public ILanguagesApi Languages { get; } = new LanguagesApi( http );
    public IStationsApi Stations { get; } = new StationsApi( http, queryStringPool );
    public ITagsApi Tags { get; } = new TagsApi( http );
}

sealed file class CountriesApi( HttpClient http ) : ICountriesApi
{
    public IAsyncEnumerable<Country> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "countries", AppJsonContext.Default.Country, cancellation )!;
}

sealed file class LanguagesApi( HttpClient http ) : ILanguagesApi
{
    public IAsyncEnumerable<Language> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "languages", AppJsonContext.Default.Language, cancellation )!;
}

sealed file class StationsApi( HttpClient http, ObjectPool<QueryStringBuilder> queryStringPool ) : IStationsApi
{
    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default ) => await http.GetFromJsonAsync( $"stations/{stationId}", AppJsonContext.Default.Station, cancellation );

    public Task<Station?> Random( CancellationToken cancellation = default ) => http.GetFromJsonAsync( "stations/random", AppJsonContext.Default.Station, cancellation );

    public async IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation )
    {
        var query = queryStringPool.Get()
            .Append( nameof( parameters.Codec ), ( int? )parameters.Codec )
            .Append( nameof( parameters.Count ), parameters.Count )
            .Append( nameof( parameters.CountryCode ), parameters.CountryCode )
            .Append( nameof( parameters.LanguageCode ), parameters.LanguageCode )
            .Append( nameof( parameters.Name ), parameters.Name )
            .Append( nameof( parameters.Offset ), parameters.Offset )
            .Append( nameof( parameters.Order ), ( int? )parameters.Order )
            .Append( nameof( parameters.Reverse ), parameters.Reverse )
            .Append( nameof( parameters.Tag ), parameters.Tag )
            .Append( nameof( parameters.Tags ), parameters.Tags );

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

    public async Task<bool> Track( Guid stationId, CancellationToken cancellation )
    {
        using var response = await http.PostAsync( $"stations/{stationId}/track", default, cancellation );
        return await response.Content.ReadFromJsonAsync( AppJsonContext.Default.Boolean, cancellation ) is true;
    }

    public async Task<bool> Vote( Guid stationId, CancellationToken cancellation )
    {
        using var response = await http.PostAsync( $"stations/{stationId}/vote", default, cancellation );
        return await response.Content.ReadFromJsonAsync( AppJsonContext.Default.Boolean, cancellation ) is true;
    }
}

sealed file class TagsApi( HttpClient http ) : ITagsApi
{
    public IAsyncEnumerable<Tag> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "tags", AppJsonContext.Default.Tag, cancellation )!;
}