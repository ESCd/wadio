using System.Globalization;
using Humanizer;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "donate", "Show your support." )]
    public async Task Donate( )
    {
        var context = await contextFactory.Create();
        await RespondAsync( InteractionCallback.Message( new()
        {
            Components = [ DonateComponent.Create( context ) ],
            Flags = MessageFlags.IsComponentsV2 | MessageFlags.Ephemeral
        } ) );
    }

    [SubSlashCommand( "ping", "Hello..? HELLO??" )]
    public static string Ping( ) => $"I'm here. {WadioEmoji.MonkeyAtPeace}";

    [SubSlashCommand( "version", "Get the current version of Wadio." )]
    public async Task Version( )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        var release = await GetRelease( api );
        if( release is null )
        {
            await FollowupAsync( new()
            {
                Components = [ VersionComponent.Create(
                    await contextFactory.Create(),
                    WadioVersion.Current ) ],
                Flags = MessageFlags.IsComponentsV2
            } );

            return;
        }

        await FollowupAsync( new()
        {
            Components = [ VersionComponent.Create(
                await contextFactory.Create(),
                release ) ],
            Flags = MessageFlags.IsComponentsV2,
        } );

        static async ValueTask<Release?> GetRelease( IWadioApi api )
        {
            ArgumentNullException.ThrowIfNull( api );

            try
            {
                return await api.Releases.Get()
                    .SingleOrDefaultAsync( release => release.Version == WadioVersion.Current );
            }
            catch
            {
                return default;
            }
        }
    }
}

static file class DonateComponent
{
    public static IMessageComponentProperties Create( ComponentCreationContext context )
    {
        ArgumentNullException.ThrowIfNull( context );

        return new ComponentContainerProperties
        {
            AccentColor = WadioColor.Default,
            Components = [
            new TextDisplayProperties(FormatContent(context)),
            new ActionRowProperties([
                new LinkButtonProperties("https://www.buymeacoffee.com/cryptoc1", "Buy me a coffee", WadioEmoji.BuyMeCoffee),
                new LinkButtonProperties("https://ko-fi.com/cryptoc1", "Support on Ko-fi", WadioEmoji.Kofi)])]
        };

        static string FormatContent( ComponentCreationContext context )
        {
            ArgumentNullException.ThrowIfNull( context );

            var builder = context.StringBuilders.Get();
            try
            {
                return builder.AppendLine( "# Support Wadio" )
                    .AppendLine( "Wadio aims to remain 100% free and open-source. Your support helps keep it that way." )
                    .ToString();
            }
            finally
            {
                context.StringBuilders.Return( builder );
            }
        }
    }
}

static file class VersionComponent
{
    public static ComponentContainerProperties Create( ComponentCreationContext context, Release release )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( release );

        return Create( context, release.Version )
            .WithComponents( [
                new ComponentSectionProperties(
                    new LinkButtonProperties(release.Url.AbsoluteUri, "View"),
                    [new TextDisplayProperties(FormatContent(context, release)),
                     new TextDisplayProperties($"-# {release.PublishedAt.Humanize()}")])] );

        static string FormatContent( ComponentCreationContext context, Release release )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( release );

            var builder = context.StringBuilders.Get();
            try
            {
                return builder.AppendLine( CultureInfo.InvariantCulture, $"# `v{release.Version}`" )
                    .AppendLine()
                    .AppendLine( release.Notes )
                    .ToString();
            }
            finally
            {
                context.StringBuilders.Return( builder );
            }
        }
    }

    public static ComponentContainerProperties Create( ComponentCreationContext context, WadioVersion version )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( version );

        return new ComponentContainerProperties
        {
            AccentColor = WadioColor.Default,
            Components = [ new TextDisplayProperties( $"# `v{version}`\n\nRelease notes were not found." ) ]
        };
    }
}