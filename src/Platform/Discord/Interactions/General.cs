using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure.Playback;

namespace Wadio.Platform.Discord.Interactions;

[SlashCommand( "wadio", "Wadio Commands." )]
internal sealed partial class WadioCommands(
    IWadioApi api,
    IComponentContextFactory contextFactory,
    StationPlayerContext stationPlayer ) : ApplicationCommandModule<ApplicationCommandContext>;