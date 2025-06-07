using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureRequestTimeouts : IConfigureOptions<RequestTimeoutOptions>
{
    public void Configure( RequestTimeoutOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.DefaultPolicy = new()
        {
            Timeout = TimeSpan.FromMinutes( 2.5 )
        };
    }
}