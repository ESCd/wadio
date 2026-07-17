using System.Diagnostics.CodeAnalysis;
using Wadio.Platform.Web.UI.Interop;

namespace Wadio.Platform.Web.UI.Infrastructure.Imports;

internal static class StationExtensions
{
    public static bool TryGetLocation( this Api.Abstractions.Station station, [NotNullWhen( true )] out Coordinate? coordinate )
    {
        ArgumentNullException.ThrowIfNull( station );

        if( station.Latitude.HasValue && station.Longitude.HasValue )
        {
            coordinate = (station.Latitude.Value, station.Longitude.Value);
            return true;
        }

        coordinate = default;
        return false;
    }
}