using Microsoft.Extensions.Options;

namespace Wadio.Extensions.CloudflareApi;

public sealed class CloudflareImageLink( IOptions<CloudflareImagesApiOptions> options )
{
    public Uri Create( string imageId, string? variant = default )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( imageId );
        if( !string.IsNullOrEmpty( variant ) )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace( variant );
        }

        var url = new Uri( $"https://imagedelivery.net/{options.Value.AccountHash}/{imageId}/" );
        if( !string.IsNullOrWhiteSpace( variant ) )
        {
            return new Uri( url, variant );
        }

        return new( url, "public" );
    }
}