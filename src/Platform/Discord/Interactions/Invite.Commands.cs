using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "invite", "Invite a friend to listen with you in your current voice channel." )]
    public async Task Invite( [SlashCommandParameter( Description = "The user to invite." )] User? user = default )
    {
        if( user is null )
        {
            await RespondAsync( InteractionCallback.Modal( InviteModal.Create() ) );
            return;
        }

        await InviteInteraction.Invite(
            Context,
            contextFactory,
            stationPlayer,
            user );
    }
}

internal sealed class InviteUserCommand(
    IComponentContextFactory contextFactory,
    StationPlayerContext stationPlayer ) : ApplicationCommandModule<ApplicationCommandContext>
{
    [UserCommand( "Invite", DefaultGuildPermissions = Permissions.UseApplicationCommands )]
    public Task Invite( User user ) => InviteInteraction.Invite(
        Context,
        contextFactory,
        stationPlayer,
        user );
}