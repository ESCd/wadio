using Microsoft.Extensions.Options;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureRouting : IConfigureOptions<RouteOptions>
{
    public void Configure( RouteOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AppendTrailingSlash = false;
        options.LowercaseUrls = true;
    }
}