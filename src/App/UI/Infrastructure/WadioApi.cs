using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;
using Wadio.App.Abstractions.Json;

namespace Wadio.App.UI.Infrastructure;

internal sealed class WadioApi( ObjectPool<QueryStringBuilder> builders, HttpClient http ) : IWadioApi
{
    public ICountriesApi Countries { get; } = new CountriesApi( http );
    public ILanguagesApi Languages { get; } = new LanguagesApi( http );
    public IReleasesApi Releases { get; } = new ReleasesApi( http );
    public IStationsApi Stations { get; } = new StationsApi( builders, http );
    public ITagsApi Tags { get; } = new TagsApi( http );

    public async ValueTask<WadioVersion> Version( CancellationToken cancellation = default ) => (await http.GetFromJsonAsync(
        "version",
        AppJsonContext.Default.WadioVersion,
        cancellation ))!;
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

sealed file class ReleasesApi( HttpClient http ) : IReleasesApi
{
    public IAsyncEnumerable<Release> Get( CancellationToken cancellation = default )
        => http.GetFromJsonAsAsyncEnumerable( "releases", AppJsonContext.Default.Release, cancellation )!;
}

sealed file class StationsApi( ObjectPool<QueryStringBuilder> builders, HttpClient http ) : IStationsApi
{
    public async ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default ) => await http.GetFromJsonAsync( $"stations/{stationId}", AppJsonContext.Default.Station, cancellation );

    public Task<Station?> Random( SearchStationsParameters? parameters = default, CancellationToken cancellation = default )
    {
        var query = BuildSearchQuery( builders, parameters ?? new() );
        return http.GetFromJsonAsync( $"stations/random{query}", AppJsonContext.Default.Station, cancellation );
    }

    public async IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, [EnumeratorCancellation] CancellationToken cancellation )
    {
        var query = BuildSearchQuery( builders, parameters );
        await foreach( var station in http.GetFromJsonAsAsyncEnumerable( $"stations{query}", AppJsonContext.Default.Station, cancellation ) )
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
        return await response.Content.ReadFromJsonAsync( AppJsonContext.Default.Boolean, cancellation ) is true;
    }

    public async Task<bool> Vote( Guid stationId, CancellationToken cancellation )
    {
        using var response = await http.PostAsync( $"stations/{stationId}/vote", default, cancellation );
        return await response.Content.ReadFromJsonAsync( AppJsonContext.Default.Boolean, cancellation ) is true;
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
            return query.Append( nameof( parameters.Codec ), ( int? )parameters.Codec )
                .Append( nameof( parameters.Count ), parameters.Count )
                .Append( nameof( parameters.CountryCode ), parameters.CountryCode )
                .Append( nameof( parameters.HasLocation ), parameters.HasLocation )
                .Append( nameof( parameters.LanguageCode ), parameters.LanguageCode )
                .Append( nameof( parameters.Name ), parameters.Name )
                .Append( nameof( parameters.Offset ), parameters.Offset )
                .Append( nameof( parameters.Order ), ( int? )parameters.Order )
                .Append( nameof( parameters.Proximity ), parameters.Proximity?.ToString() )
                .Append( nameof( parameters.Reverse ), parameters.Reverse )
                .Append( nameof( parameters.Tag ), parameters.Tag )
                .Append( nameof( parameters.Tags ), parameters.Tags )
                .ToString();
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
        => http.GetFromJsonAsAsyncEnumerable( "tags", AppJsonContext.Default.Tag, cancellation )!;
}