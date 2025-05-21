using Microsoft.Extensions.Options;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureCookiePolicy : IConfigureOptions<CookiePolicyOptions>
{
    public void Configure( CookiePolicyOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
    }
}