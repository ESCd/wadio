using System.Globalization;
using System.Text;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure;

internal static class StationFormatter
{
    public static StringBuilder AppendComponentMarkdown( this StringBuilder builder, ComponentCreationContext context, Station station )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( station );

        return builder.AppendLine( CultureInfo.InvariantCulture, $"## [{station.Name}]({context.CreateStationUrl( station )})" )
            .Append( CultureInfo.InvariantCulture, $"\t{WadioEmoji.LocationChip}\t" ).AppendLocations( station ).AppendLine()
            .AppendLine( CultureInfo.InvariantCulture, $"\t{WadioEmoji.VotingChip}\t{station.Metrics.Votes} votes" )
            .AppendLine( CultureInfo.InvariantCulture, $"\t{WadioEmoji.MusicCast}\t{station.Metrics.Plays} plays" );
    }

    public static StringBuilder AppendLanguages( this StringBuilder builder, Station station, int? max = default )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( station );

        if( station.Languages.Length is 0 )
        {
            return builder.Append( "N/A" );
        }

        if( max.HasValue )
        {
            if( station.Languages.Length >= max.Value )
            {
                return builder.AppendJoin(
                    ", ",
                    station.Languages.Take( Math.Max( 0, max.Value - 1 ) ).Append( "..." ) );
            }
        }

        return builder.AppendJoin( ", ", station.Languages );
    }

    public static StringBuilder AppendLocations( this StringBuilder builder, Station station )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( station );

        var country = !string.IsNullOrWhiteSpace( station.Country );
        var code = !string.IsNullOrWhiteSpace( station.CountryCode );

        if( !string.IsNullOrWhiteSpace( station.State ) )
        {
            builder.Append( station.State );
            if( country || code )
            {
                builder.Append( ", " );
            }
        }

        if( country )
        {
            builder.Append( station.Country );
            if( code )
            {
                return builder.Append(
                    CultureInfo.InvariantCulture,
                    $" ({station.CountryCode})" );
            }
        }

        if( code )
        {
            return builder.Append( station.CountryCode );
        }

        return builder.Append( "N/A" );
    }

    public static StringBuilder AppendTags( this StringBuilder builder, Station station, int? max = default )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( station );

        if( station.Tags.Length is 0 )
        {
            return builder.Append( "N/A" );
        }

        if( max.HasValue )
        {
            if( station.Tags.Length >= max.Value )
            {
                return builder.AppendJoin(
                    ", ",
                    station.Tags.Take( Math.Max( 0, max.Value - 1 ) ).Append( "..." ) );
            }
        }

        return builder.AppendJoin( ", ", station.Tags );
    }

    public static string FormatLocation( Station station )
    {
        ArgumentNullException.ThrowIfNull( station );

        return AppendLocations( new( (station.Country?.Length + station.CountryCode?.Length) ?? 0 ), station ).ToString();
    }

    public static string FormatTags( Station station, int? max = default )
    {
        ArgumentNullException.ThrowIfNull( station );

        return AppendTags( new(), station, max ).ToString();
    }
}