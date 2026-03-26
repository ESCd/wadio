using NetCord;
using NetCord.Rest;
using NetCord.Services;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;

namespace Wadio.Platform.Discord.Interactions;

internal static class StationInteraction
{
    public static async Task<RestMessage> Info<TContext>(
        TContext context,
        IWadioApi api,
        IComponentContextFactory componentContextFactory,
        Guid stationId )
        where TContext : class, IInteractionContext, IGuildContext
    {
        ArgumentNullException.ThrowIfNull( context );

        var station = await api.Stations.Get( stationId );
        if( station is null )
        {
            return await context.Interaction.SendFollowupMessageAsync( new()
            {
                Content = "Station not found.",
                Flags = MessageFlags.Ephemeral,
            } );
        }

        return await context.Interaction.SendFollowupMessageAsync( new()
        {
            Components = [ StationComponent.Create(
                await componentContextFactory.Create(),
                station ) ],
            Flags = MessageFlags.IsComponentsV2,
        } );
    }
}