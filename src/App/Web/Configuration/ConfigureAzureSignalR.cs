using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureAzureSignalR : IConfigureOptions<ServiceOptions>
{
    public void Configure( ServiceOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.GracefulShutdown = new()
        {
            Mode = GracefulShutdownMode.MigrateClients
        };

        options.ServerStickyMode = ServerStickyMode.Preferred;
    }
}