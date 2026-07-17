using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Abstractions;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "info", "Display details about a Station." )]
    public async Task Info( [SlashCommandParameter( Description = "The ID or URL of the Station." )] StationId stationId )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        await StationInteraction.Info(
            Context,
            api,
            contextFactory,
            stationId );
    }

    [SubSlashCommand( "random", "Display details about a random Station." )]
    public async Task Random( )
    {
        await RespondAsync( InteractionCallback.DeferredMessage( MessageFlags.Ephemeral ) );

        var station = await api.Stations.Random();

        await StationInteraction.Info(
            Context,
            api,
            contextFactory,
            station!.Id );
    }
}