using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Humanizer;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal static class InviteInteraction
{
    public static async Task<RestMessage> Invite<TContext>(
        TContext context,
        IComponentContextFactory componentContextFactory,
        StationPlayerContext stationPlayer,
        User user )
        where TContext : class, IInteractionContext, IGatewayClientContext, IGuildContext, IUserContext
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( componentContextFactory );
        ArgumentNullException.ThrowIfNull( stationPlayer );

        await context.Interaction.SendResponseAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        if( !TryGetActiveChannel( context, out var channel ) )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "You must be listening in a voice channel to use this command.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        if( IsUserActive( context, user, channel.Id ) )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = $"{user} is already listening in the voice channel.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        _ = await (await user.GetDMChannelAsync()).SendMessageAsync( new()
        {
            Components = [InviteComponent.Create(
                await componentContextFactory.Create(),
                context.User,
                await context.Client.Rest.CreateGuildChannelInviteAsync( channel.Id, new()
                {
                    MaxUses = 1,
                    Unique = true,
                } ),
                stationPlayer.Status(context.Guild!.Id))],
            Flags = MessageFlags.IsComponentsV2,
        } );

        return await context.Interaction.SendFollowupMessageAsync( new()
        {
            Content = $"An invite has been sent to {user}.",
            Flags = MessageFlags.Ephemeral,
        } );

        static bool IsUserActive( TContext context, User user, ulong channelId )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( user );
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero( channelId );

            if( !context.TryGetUserVoiceChannel( user.Id, out var channel ) )
            {
                return false;
            }

            return channel.Id == channelId;
        }
    }

    public static Task<InteractionCallbackResponse?> RespondNotListening<TContext>( TContext context )
        where TContext : class, IInteractionContext, IGuildContext, IUserContext
    {
        ArgumentNullException.ThrowIfNull( context );

        return context.Interaction.SendResponseAsync( InteractionCallback.Message( new()
        {
            Content = "You must be listening in a voice channel to use this command.",
            Flags = MessageFlags.Ephemeral,
        } ) );
    }

    public static bool TryGetActiveChannel<TContext>( TContext context, [NotNullWhen( true )] out VoiceGuildChannel? channel )
        where TContext : class, IInteractionContext, IGuildContext
    {
        ArgumentNullException.ThrowIfNull( context );

        if( !context.Interaction.GuildId.HasValue )
        {
            channel = default;
            return false;
        }

        return context.TryGetUserVoiceChannel( context.Interaction.ApplicationId, out channel );
    }
}

internal static class InviteComponent
{
    public static IMessageComponentProperties Create( ComponentCreationContext context, User user, RestInvite invite, StationPlayerStatus? status )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( user );
        ArgumentNullException.ThrowIfNull( invite );

        return new ComponentContainerProperties
        {
            AccentColor = WadioColor.Default,
            Components = [new ComponentSectionProperties(
                new LinkButtonProperties($"https://discord.gg/{invite.Code}", "Join", WadioEmoji.PlayCircle),
                [new TextDisplayProperties(FormatContent(context, user, invite, status))])]
        };

        static string FormatContent( ComponentCreationContext context, User user, RestInvite invite, StationPlayerStatus? status )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( user );
            ArgumentNullException.ThrowIfNull( invite );

            var builder = context.StringBuilders.Get();
            try
            {
                return builder.AppendLine( "## You're Invited to Listen Along!" )
                    .AppendLine( CultureInfo.InvariantCulture, $"{user} has invited you to listen to ***{status?.Station.Name ?? "N/A"}***." )
                    .AppendLine()
                    .AppendLine( invite.Channel!.ToString() )
                    .AppendLine()
                    .AppendLine( CultureInfo.InvariantCulture, $"-# expires {invite.ExpiresAt.Humanize()}" )
                    .ToString();
            }
            finally
            {
                context.StringBuilders.Return( builder );
            }
        }
    }
}