using System.Diagnostics.CodeAnalysis;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Infrastructure;

internal static class StationExtensions
{
    public static bool TryGetLocation( this Abstractions.Api.Station station, [NotNullWhen( true )] out Coordinate? coordinate )
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