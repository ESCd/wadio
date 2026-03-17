using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Wadio.Platform.Hosting.Configuration;

internal sealed class ConfigureRouting : IConfigureOptions<RouteOptions>
{
    public void Configure( RouteOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AppendTrailingSlash = false;
        options.LowercaseUrls = true;
    }
}