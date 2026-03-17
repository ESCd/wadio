using System.Threading.Channels;
using Microsoft.Extensions.Options;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Interactions;

[SlashCommand( "wadio", "Wadio Commands." )]
internal sealed partial class WadioCommands(
    IWadioApi api,
    IOptionsMonitor<WadioBotOptions> optionsMonitor,
    Channel<StationPlayerRequest> queue ) : ApplicationCommandModule<ApplicationCommandContext>;