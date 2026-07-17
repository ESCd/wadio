using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Wadio.Platform.Api.Infrastructure;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureHubs : IConfigureOptions<HubOptions>
{
    public void Configure( HubOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddFilter<HubCancellationFilter>();
    }
}