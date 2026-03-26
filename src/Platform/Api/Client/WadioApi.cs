using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Abstractions.Json;

namespace Wadio.Platform.Api.Client;

internal sealed class WadioApi( ObjectPool<QueryStringBuilder> builders, HttpClient http ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( http );
    public ILanguagesApi Languages { get; } = new LanguagesApi( http );
    public IReleasesApi Releases { get; } = new ReleasesApi( http );
    public IStationsApi Stations { get; } = new StationsApi( builders, http );
    public ITagsApi Tags { get; } = new TagsApi( http );

    public async ValueTask<WadioVersion> Version( CancellationToken cancellation = default ) => (await http.GetFromJsonAsync(
        "version",
        ApiJsonContext.Default.WadioVersion,
        cancellation ))!;
}

sealed file class CountriesApi( HttpClient http ) : ICountriesApi
{
    public IAsyncEnumerable<Country> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "countries", ApiJsonContext.Default.Country, cancellation )!;
}

sealed file class LanguagesApi( HttpClient http ) : ILanguagesApi
{
    public IAsyncEnumerable<Language> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "languages", ApiJsonContext.Default.Language, cancellation )!;
}

sealed file class ReleasesApi( HttpClient http ) : IReleasesApi
{
    public IAsyncEnumerable<Release> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "releases", ApiJsonContext.Default.Release, cancellation )!;
}

sealed file class StationsApi( ObjectPool<QueryStringBuilder> builders, HttpClient http ) : IStationsApi
{
    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default ) => await http.GetFromJsonAsync( $"stations/{stationId}", ApiJsonContext.Default.Station, cancellation );

    public Task<Station?> Random( SearchStationsParameters? parameters = default, CancellationToken cancellation = default )
    {
        var query = BuildSearchQuery( builders, parameters ?? new() );
        return http.GetFromJsonAsync( $"stations/random{query}", ApiJsonContext.Default.Station, cancellation );
    }

    public async IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation )
    {
        var query = BuildSearchQuery( builders, parameters );
        await foreach( var station in http.GetFromJsonAsAsyncEnumerable( $"stations{query}", ApiJsonContext.Default.Station, cancellation ) )
        {
            if( station is not null )
            {
                yield return station;
            }
        }
    }

    public async Task<bool> Track( Guid stationId, CancellationToken cancellation )
    {
        using var response = await http.PostAsync( $"stations/{stationId}/track", default, cancellation );
        return await response.Content.ReadFromJsonAsync( ApiJsonContext.Default.Boolean, cancellation ) is true;
    }

    public async Task<bool> Vote( Guid stationId, CancellationToken cancellation )
    {
        using var response = await http.PostAsync( $"stations/{stationId}/vote", default, cancellation );
        return await response.Content.ReadFromJsonAsync( ApiJsonContext.Default.Boolean, cancellation ) is true;
    }

    private static string BuildSearchQuery( ObjectPool<QueryStringBuilder> builders, SearchStationsParameters? parameters )
    {
        ArgumentNullException.ThrowIfNull( builders );
        if( parameters is null )
        {
            return string.Empty;
        }

        var query = builders.Get();
        try
        {
            return query.AppendSearchParameters( parameters ).ToString();
        }
        finally
        {
            builders.Return( query );
        }
    }
}

sealed file class TagsApi( HttpClient http ) : ITagsApi
{
    public IAsyncEnumerable<Tag> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "tags", ApiJsonContext.Default.Tag, cancellation )!;
}