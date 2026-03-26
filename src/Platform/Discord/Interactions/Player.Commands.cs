using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "play", "Play a Station in your current voice channel." )]
    public async Task Play( [SlashCommandParameter( Description = "The ID or URL of the Station." )] StationId stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage() );

        await PlayerInteraction.Play(
            Context,
            api,
            contextFactory,
            stationPlayer,
            stationId );
    }

    [SubSlashCommand( "status", "Get the current playback status in the server." )]
    public async Task Status( )
    {
        if( !Context.Interaction.GuildId.HasValue )
        {
            await RespondAsync( InteractionCallback.Message( new()
            {
                Content = "This command can only be used in a server.",
                Flags = MessageFlags.Ephemeral,
            } ) );

            return;
        }

        if( !Context.TryGetUserVoiceChannel( Context.Interaction.ApplicationId, out _ ) )
        {
            await RespondAsync( InteractionCallback.Message( new()
            {
                Content = "Wadio is not in use.",
                Flags = MessageFlags.Ephemeral,
            } ) );

            return;
        }

        await RespondAsync( InteractionCallback.DeferredMessage() );

        await stationPlayer.Status(
            Context.Interaction.GuildId.Value,
            async status => await FollowupAsync( new()
            {
                Components = [ PlayerComponent.Create(
                    await contextFactory.Create(),
                    status ) ],
                Flags = MessageFlags.IsComponentsV2
            } ) );
    }

    [SubSlashCommand( "stop", "Stop playback in the current server." )]
    public Task Stop( ) => PlayerInteraction.Stop( Context, stationPlayer );
}