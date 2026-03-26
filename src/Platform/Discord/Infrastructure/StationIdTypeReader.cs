using NetCord;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure;

internal sealed class StationIdTypeReader : SlashCommandTypeReader<ApplicationCommandContext>
{
    public override ApplicationCommandOptionType Type => ApplicationCommandOptionType.String;

    public override ValueTask<SlashCommandTypeReaderResult> ReadAsync(
        string value,
        ApplicationCommandContext context,
        SlashCommandParameter<ApplicationCommandContext> parameter,
        ApplicationCommandServiceConfiguration<ApplicationCommandContext> configuration,
        IServiceProvider? serviceProvider )
    {
        if( StationIdParser.TryParse( value, out var stationId ) )
        {
            return new( SlashCommandTypeReaderResult.Success( new StationId( stationId ) ) );
        }

        return new( SlashCommandTypeReaderResult.Fail( $"Failed to parse '{parameter.Name}': Invalid Station Id or Url." ) );
    }
}

internal static class StationIdParser
{
    public static bool TryParse( string? stationId, out Guid value )
    {
        stationId = stationId?.Trim();
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