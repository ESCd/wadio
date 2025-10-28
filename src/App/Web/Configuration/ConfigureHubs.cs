using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Wadio.App.Web.Infrastructure;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureHubs : IConfigureOptions<HubOptions>
{
    public void Configure( HubOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddFilter<HubCancellationFilter>();
    }
}