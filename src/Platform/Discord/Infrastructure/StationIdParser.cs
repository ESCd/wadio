namespace Wadio.Platform.Discord.Infrastructure;

internal static class StationIdParser
{
    public static bool TryParse( string stationId, out Guid value )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( stationId );

        if( Guid.TryParse( stationId, out value ) )
        {
            return true;
        }

        if( Uri.TryCreate( stationId, UriKind.Absolute, out var url ) )
        {
            var path = PathString.FromUriComponent( url );
            if( path.StartsWithSegments( "/station", out var remaining ) )
            {
                return Guid.TryParse(
                    remaining.ToUriComponent().Trim( '/' ),
                    out value );
            }
        }

        value = default;
        return false;
    }
}