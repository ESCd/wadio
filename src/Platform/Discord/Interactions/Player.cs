using NetCord;
using NetCord.Rest;
using NetCord.Services;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal static class PlayerInteraction
{
    public static async Task<RestMessage> Play<TContext>(
        TContext context,
        IWadioApi api,
        IComponentContextFactory componentContextFactory,
        StationPlayerContext stationPlayer,
        Guid stationId )
        where TContext : class, IInteractionContext, IGuildContext, IUserContext
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( componentContextFactory );
        ArgumentNullException.ThrowIfNull( stationPlayer );

        if( !context.TryGetUserVoiceChannel( context.User.Id, out var channel ) )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "You must be in a voice channel to use this command.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        var station = await api.Stations.Get( stationId );
        if( station is null )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "Station not found.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        if( station.IsHls )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "HLS playback is currently unsupported.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        var message = await stationPlayer.Play(
           channel,
           station.Id,
           async status => await context.Interaction.SendFollowupMessageAsync( new()
           {
               Components = [ PlayerComponent.Create(
                    await componentContextFactory.Create(),
                    status ) ],
               Flags = MessageFlags.IsComponentsV2
           } ) );

        await api.Stations.Track( station.Id );
        return message;
    }

    public static async Task<RestMessage> Stop<TContext>( TContext context, StationPlayerContext stationPlayer )
        where TContext : class, IInteractionContext, IGuildContext
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( stationPlayer );

        await context.Interaction.SendResponseAsync( InteractionCallback.DeferredMessage() );

        if( !context.Interaction.GuildId.HasValue )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "This command can only be used in a server.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        if( !context.TryGetUserVoiceChannel( context.Interaction.ApplicationId, out _ ) )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "Wadio is not in use.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        await stationPlayer.Stop( context.Interaction.GuildId.Value );
        return await context.Interaction.SendFollowupMessageAsync( new()
        {
            Content = "Wadio has been disconnected."
        } );
    }
}