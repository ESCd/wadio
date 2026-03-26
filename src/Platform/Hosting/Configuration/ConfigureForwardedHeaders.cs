using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Wadio.Platform.Hosting.Configuration;

public sealed class ConfigureForwardedHeaders : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure( ForwardedHeadersOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedHost
            | ForwardedHeaders.XForwardedPrefix
            | ForwardedHeaders.XForwardedProto;
    }
}