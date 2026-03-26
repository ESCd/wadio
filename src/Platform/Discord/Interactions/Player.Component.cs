using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
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
            if( !MetadataFormatter.TryGetThumbnail( status, out var thumbnail ) )
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
                        .Append( "### *" ).AppendTitle( status.Meta ).AppendLine( "*" )
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

static file class MetadataFormatter
{
    public static StringBuilder AppendTitle( this StringBuilder builder, IReadOnlyDictionary<string, string>? meta )
    {
        ArgumentNullException.ThrowIfNull( builder );

        if( TryGetValue(
            meta,
            [ "Title", "StreamTitle", "StreamName", "Name" ],
            out var title ) )
        {
            return builder.Append( title );
        }

        return builder.Append( "N/A" );
    }

    public static bool TryGetThumbnail( StationPlayerStatus? status, [NotNullWhen( true )] out Uri? url )
    {
        if( status?.Station.IconUrl is null && status?.Meta is null )
        {
            url = default;
            return false;
        }

        if( TryGetUrl(
            status.Meta,
            [ "StreamArtwork", "StreamCover", "Artwork", "ArtworkUrl", "Cover", "CoverUrl", "Image" ],
            out url ) )
        {
            return true;
        }

        if( status?.Station.IconUrl is not null )
        {
            url = status.Station.IconUrl;
            return true;
        }

        return false;

        static bool TryGetUrl( IReadOnlyDictionary<string, string>? meta, IEnumerable<string> keys, [NotNullWhen( true )] out Uri? url )
        {
            ArgumentNullException.ThrowIfNull( keys );

            foreach( var key in keys )
            {
                if( TryGetValue( meta, key, out var value ) is true && Uri.TryCreate( value, UriKind.Absolute, out url ) )
                {
                    return true;
                }
            }

            url = default;
            return false;
        }
    }

    private static bool TryGetValue( IReadOnlyDictionary<string, string>? source, string key, [NotNullWhen( true )] out string? value )
    {
        if( source?.TryGetValue( key, out value ) is true )
        {
            if( !string.IsNullOrEmpty( value = value?.Trim() ) )
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetValue( IReadOnlyDictionary<string, string>? source, IEnumerable<string> keys, [NotNullWhen( true )] out string? value )
    {
        ArgumentNullException.ThrowIfNull( keys );

        foreach( var key in keys )
        {
            if( TryGetValue( source, key, out value ) is true )
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}