using System.Globalization;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "info", "Display details about a Station." )]
    public async Task Show( [SlashCommandParameter( Description = "The ID or URL of the Station." )] string value )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        if( !StationIdParser.TryParse( value, out var stationId ) )
        {
            await FollowupAsync( new()
            {
                Content = "Invalid Station ID or URL.",
                Flags = MessageFlags.Ephemeral,
            } );

            return;
        }

        var station = await api.Stations.Get( stationId );
        if( station is null )
        {
            await FollowupAsync( new()
            {
                Content = "Station not found.",
                Flags = MessageFlags.Ephemeral,
            } );

            return;
        }

        await FollowupAsync( new()
        {
            Embeds = [ StationEmbed.Create(
                Context.Interaction,
                optionsMonitor.CurrentValue,
                station ) ]
        } );
    }
}

static file class StationEmbed
{
    public static EmbedProperties Create( Interaction interaction, WadioBotOptions options, Station station )
    {
        ArgumentNullException.ThrowIfNull( interaction );
        ArgumentNullException.ThrowIfNull( options );
        ArgumentNullException.ThrowIfNull( station );

        return new EmbedProperties()
            .WithFields( EnumerateFields( station ) )
            .WithTimestamp( station.CheckedAt ?? station.UpdatedAt )
            .WithTitle( $"***{station.Name}***" )
            .WithThumbnail( station.IconUrl?.AbsoluteUri )
            .WithUrl( new Uri( options.WebEndpoint, $"station/{station.Id}" ).AbsoluteUri )
            .WithUserColor( interaction );

        static IEnumerable<EmbedFieldProperties> EnumerateFields( Station station )
        {
            ArgumentNullException.ThrowIfNull( station );

            yield return new()
            {
                Inline = true,
                Name = "Plays",
                Value = station.Metrics.Plays.ToString( CultureInfo.InvariantCulture ),
            };

            yield return new()
            {
                Inline = true,
                Name = "Votes",
                Value = station.Metrics.Votes.ToString( CultureInfo.InvariantCulture ),
            };

            yield return new();

            yield return new()
            {
                Inline = true,
                Name = "Codec",
                Value = CodecString.Format( station.Codec )
            };

            yield return new()
            {
                Inline = true,
                Name = "Bitrate",
                Value = station.Bitrate is not null ? $"{station.Bitrate} Kb/s" : "N/A",
            };

            yield return new()
            {
                Name = "HLS",
                Value = station.IsHls ? "Yes" : "No"
            };

            yield return new()
            {
                Name = "Country",
                Value = FormatCountry( station ),
            };

            yield return new()
            {
                Name = "Languages",
                Value = station.Languages.Length is not 0 ? string.Join( ", ", station.Languages ) : "N/A",
            };

            yield return new()
            {
                Name = "Tags",
                Value = station.Tags.Length is not 0 ? string.Join( ", ", station.Tags ) : "N/A",
            };

            static string FormatCountry( Station station )
            {
                ArgumentNullException.ThrowIfNull( station );

                var country = !string.IsNullOrWhiteSpace( station.Country );
                var code = !string.IsNullOrWhiteSpace( station.CountryCode );

                if( country && code )
                {
                    return $"{station.Country} ({station.CountryCode})";
                }

                if( country )
                {
                    return station.Country!;
                }

                if( code )
                {
                    return station.CountryCode!;
                }

                return "N/A";
            }
        }
    }
}