using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;

namespace Wadio.Platform.Api.Configuration;

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