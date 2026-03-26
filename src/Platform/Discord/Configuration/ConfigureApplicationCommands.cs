using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord.Configuration;

internal sealed class ConfigureApplicationCommands : IConfigureOptions<ApplicationCommandServiceOptions<ApplicationCommandInteraction, ApplicationCommandContext, AutocompleteInteractionContext>>
{
    public void Configure( ApplicationCommandServiceOptions<ApplicationCommandInteraction, ApplicationCommandContext, AutocompleteInteractionContext> options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.TypeReaders.Add( typeof( StationId ), new StationIdTypeReader() );
    }
}