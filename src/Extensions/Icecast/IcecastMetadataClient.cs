using System.Globalization;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.Extensions.Icecast;

public sealed class IcecastMetadataClient( HttpClient http )
{
    public Task<IcecastMetadataReader> GetReader( Uri url, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( url );
        return http.GetIcecastReader( url, cancellation );
    }
}

public static class IcecastRequestExtensions
{
    public static async Task<IcecastMetadataReader> GetIcecastReader( this HttpClient http, Uri url, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( http );
        ArgumentNullException.ThrowIfNull( url );

        using var request = new HttpRequestMessage( HttpMethod.Get, url )
        {
            Headers = { { IcecastHeaderNames.MetaData, "1" } }
        };

        // NOTE: no `using` here, the reader owns the response
        var response = (await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellation ).ConfigureAwait( false )).EnsureSuccessStatusCode();

        if( !(response.Headers.TryGetValues( IcecastHeaderNames.MetaInt, out var values ) && int.TryParse( values.FirstOrDefault(), CultureInfo.InvariantCulture, out var interval ) && interval > 0) )
        {
            response.Dispose();
            throw new HttpRequestException( HttpRequestError.InvalidResponse, $"The response did not contain a valid '{IcecastHeaderNames.MetaInt}' header." );
        }

        return new(
            response,
            await response.Content.ReadAsStreamAsync( cancellation ).ConfigureAwait( false ),
            interval );
    }
}