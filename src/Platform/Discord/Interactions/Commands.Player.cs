using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "play", "Play a Station in your current voice channel." )]
    public async Task Play( [SlashCommandParameter( Description = "The ID or URL of the Station." )] string value )
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

        var channel = Context.GetUserVoiceChannel( Context.User.Id );
        if( channel is null )
        {
            await FollowupAsync( new()
            {
                Content = "You must be in a voice channel to use this command.",
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

        if( station.IsHls )
        {
            await FollowupAsync( new()
            {
                Content = "HLS playback is currently unsupported.",
                Flags = MessageFlags.Ephemeral,
            } );

            return;
        }

        await queue.Invoke( new( channel, station.Id ) );
        await FollowupAsync( new()
        {
            Content = "TODO: Player Component",
            Flags = MessageFlags.Ephemeral,
        } );
    }
}