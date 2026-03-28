using System.Globalization;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal sealed class PlayerComponent( StationPlayerContext stationPlayer ) : ComponentInteractionModule<ButtonInteractionContext>
{
    private const string StopButtonId = "player.stop";

    public static IMessageComponentProperties Create( ComponentCreationContext context, StationPlayerStatus? status )
    {
        ArgumentNullException.ThrowIfNull( context );

        return new ComponentContainerProperties
        {
            AccentColor = context.GetAccentColor( status?.Station ),
            Components = [
                CreateContent(context, status),

                new ActionRowProperties([
                    new ButtonProperties(StopButtonId, WadioEmoji.StopCircle, ButtonStyle.Danger)
                    {
                        Disabled = status?.Station is null,
                    },
                    new ButtonProperties($"{StationComponent.InfoButtonId}:{status?.Station?.Id}", WadioEmoji.ExpandCircle, ButtonStyle.Secondary)
                    {
                        Disabled = status?.Station is null,
                    }])]
        };

        static IComponentContainerComponentProperties CreateContent( ComponentCreationContext context, StationPlayerStatus? status )
        {
            ArgumentNullException.ThrowIfNull( context );

            var content = new TextDisplayProperties( FormatContent( context, status ) );

            var thumbnail = status?.Meta?.ArtworkUrl ?? status?.Station.IconUrl;
            if( thumbnail is null )
            {
                return content;
            }

            return new ComponentSectionProperties(
                new ComponentSectionThumbnailProperties( thumbnail.AbsoluteUri ),
                [ content ] );

            static string FormatContent( ComponentCreationContext context, StationPlayerStatus? status )
            {
                ArgumentNullException.ThrowIfNull( context );

                var builder = context.StringBuilders.Get();
                try
                {
                    if( status is null )
                    {
                        return builder.AppendLine( CultureInfo.InvariantCulture, $"## Not Playing..." )
                            .AppendLine( "### *N/A*" )
                            .ToString();
                    }

                    return builder.AppendComponentMarkdown( context, status.Station )
                        .AppendLine( CultureInfo.InvariantCulture, $"### *{status.Meta?.Title ?? "N/A"}*" )
                        .ToString();
                }
                finally
                {
                    context.StringBuilders.Return( builder );
                }
            }
        }
    }

    [ComponentInteraction( StopButtonId )]
    public Task Stop( ) => PlayerInteraction.Stop( Context, stationPlayer );
}