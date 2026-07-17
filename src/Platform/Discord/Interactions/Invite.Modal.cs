using System.Diagnostics.CodeAnalysis;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

internal sealed class InviteModal(
    IComponentContextFactory contextFactory,
    StationPlayerContext stationPlayer ) : ComponentInteractionModule<ModalInteractionContext>
{
    private const string ModalId = "invite";
    private const string MenuId = "invite.user";

    public static ModalProperties Create( ) => new( ModalId, "Invite a Friend" )
    {
        Components = [
            new TextDisplayProperties("Invite a friend to listen with you in your current voice channel. They will be able to join your voice channel and listen along with you."),
            new LabelProperties("User", new UserMenuProperties(MenuId)
            {
                MaxValues = 1,
                MinValues = 1,
                Required = true,
            })]
    };

    [ComponentInteraction( ModalId )]
    public Task Invite( )
    {
        if( !TryGetUser( Context, out var user ) )
        {
            return RespondAsync( InteractionCallback.Message( new()
            {
                Content = "You must select a user to invite.",
                Flags = MessageFlags.Ephemeral,
            } ) );
        }

        return InviteInteraction.Invite(
            Context,
            contextFactory,
            stationPlayer,
            user );

        static bool TryGetUser( ModalInteractionContext context, [NotNullWhen( true )] out User? user )
        {
            ArgumentNullException.ThrowIfNull( context );

            if( context.Components.Count is 0 )
            {
                user = default;
                return false;
            }

            if( context.Components[ 1 ] is Label { Component: UserMenu { CustomId: MenuId, SelectedValues.Count: 1 } menu } )
            {
                user = menu.SelectedValues[ 0 ];
                return true;
            }

            user = default;
            return false;
        }
    }
}