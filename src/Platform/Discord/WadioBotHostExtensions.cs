using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Wadio.Platform.Discord.Interactions;

namespace Wadio.Platform.Discord;

internal static class WadioBotHostExtensions
{
    public static THost UseWadioBot<THost>( this THost host )
        where THost : IHost
    {
        ArgumentNullException.ThrowIfNull( host );

        host.AddApplicationCommandModule<WadioCommands>()
            .AddComponentInteractionModule<ButtonInteractionContext, PlayerComponent>()
            .AddComponentInteractionModule<ButtonInteractionContext, SearchPagerComponent>()
            .AddComponentInteractionModule<ButtonInteractionContext, StationComponent>()
            .AddComponentInteractionModule<ModalInteractionContext, SearchModal>();

        return host;
    }
}