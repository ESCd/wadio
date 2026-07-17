using System.Globalization;
using Humanizer;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal sealed class StationComponent(
    IWadioApi api,
    IComponentContextFactory contextFactory,
    StationPlayerContext stationPlayer ) : ComponentInteractionModule<ButtonInteractionContext>
{
    public const string InfoButtonId = "station.info";
    public const string PlayButtonId = "station.play";
    private const string RelatedButtonId = "station.related";
    public const string VoteButtonId = "station.vote";

    public static IMessageComponentProperties Create( ComponentCreationContext context, Station station )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( station );

        return new ComponentContainerProperties
        {
            AccentColor = context.GetAccentColor( station ),
            Components = [
                CreateHeader(context, station),
                new TextDisplayProperties(FormatBody(context, station)),
                new TextDisplayProperties($"-# checked {station.CheckedAt.Humanize()}\t•\tupdated {station.UpdatedAt.Humanize()}"),

                new ActionRowProperties([
                    new ButtonProperties(
                        $"{PlayButtonId}:{station.Id}",
                        station.IsHls ? WadioEmoji.PlayDisabled : WadioEmoji.PlayCircle,
                        ButtonStyle.Primary )
                    {
                        Disabled = station.IsHls
                    },
                    new ButtonProperties(
                        $"{VoteButtonId}:{station.Id}",
                        WadioEmoji.ThumbsUp,
                        ButtonStyle.Secondary),

                    new ButtonProperties(
                        $"{RelatedButtonId}:{station.Id}",
                        WadioEmoji.ActionKey,
                        ButtonStyle.Secondary )
                    {
                        Disabled = station.Tags.Length is 0
                    }])]
        };

        static string FormatBody( ComponentCreationContext context, Station station )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( station );

            var builder = context.StringBuilders.Get();
            try
            {
                builder.AppendLine( CultureInfo.InvariantCulture, $"### Codec" )
                    .AppendLine( CultureInfo.InvariantCulture, $"{CodecString.Format( station.Codec )} @ {station.Bitrate?.ToString( CultureInfo.InvariantCulture ) ?? "N/A"} Kb/s" )

                    .AppendLine( CultureInfo.InvariantCulture, $"### Languages" )
                    .AppendLanguages( station ).AppendLine()

                    .AppendLine( CultureInfo.InvariantCulture, $"### Tags" )
                    .AppendTags( station ).AppendLine()
                    .AppendLine();

                return builder.ToString();
            }
            finally
            {
                context.StringBuilders.Return( builder );
            }
        }
    }

    public static IComponentContainerComponentProperties CreateHeader( ComponentCreationContext context, Station station )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( station );

        var content = new TextDisplayProperties( FormatContent( context, station ) );
        if( station.IconUrl is null )
        {
            return content;
        }

        return new ComponentSectionProperties(
            new ComponentSectionThumbnailProperties( station.IconUrl.AbsoluteUri ),
            [ content ] );

        static string FormatContent( ComponentCreationContext context, Station station )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( station );

            var builder = context.StringBuilders.Get();
            try
            {
                return builder.AppendComponentMarkdown( context, station ).ToString();
            }
            finally
            {
                context.StringBuilders.Return( builder );
            }
        }
    }

    [ComponentInteraction( InfoButtonId )]
    public async Task Info( Guid stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        await StationInteraction.Info(
            Context,
            api,
            contextFactory,
            stationId );
    }

    [ComponentInteraction( PlayButtonId )]
    public async Task Play( Guid stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage() );

        await PlayerInteraction.Play(
            Context,
            api,
            contextFactory,
            stationPlayer,
            stationId );
    }

    [ComponentInteraction( RelatedButtonId )]
    public async Task Related( Guid stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

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

        var stations = await api.Stations.Related( station, new()
        {
            Count = SearchInteraction.MaxResults
        } ).ToListAsync();

        var context = await contextFactory.Create();
        await FollowupAsync( new()
        {
            Components = RelatedStationsComponent.Create( context, station, stations ),
            Flags = MessageFlags.IsComponentsV2,
        } );
    }

    [ComponentInteraction( VoteButtonId )]
    public async Task Vote( Guid stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        await api.Stations.Vote( stationId );
        await FollowupAsync( new()
        {
            Content = "Vote has been submitted.",
            Flags = MessageFlags.Ephemeral
        } );
    }
}

static file class RelatedStationsComponent
{
    public static IEnumerable<IMessageComponentProperties> Create( ComponentCreationContext context, Station station, IReadOnlyList<Station> stations )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( station );
        ArgumentNullException.ThrowIfNull( stations );

        var components = new List<IMessageComponentProperties>( stations.Count + 1 )
        {
            new TextDisplayProperties($"## Stations Related to '{station.Name}'")
        };

        if( stations.Count is 0 )
        {
            components.Add( new ComponentContainerProperties
            {
                AccentColor = context.GetAccentColor( station ),
                Components = [ new TextDisplayProperties( "### No related stations found." ) ]
            } );

            return components;
        }

        components.AddRange( stations.Select( station => SearchPagerComponent.CreateItem( context, station ) ) );
        return components;
    }
}