using ESCd.Extensions.Caching;
using Wadio.App.UI;
using Wadio.App.UI.Abstractions;
using Wadio.App.Web.Configuration;
using Wadio.App.Web.Infrastructure;
using Wadio.Extensions.RadioBrowser;

namespace Wadio.App.Web;

internal static class WebServiceExtensions
{
    public static IServiceCollection AddWadioWeb( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddHealthChecks();
        services.AddAsyncCache()
            .AddEndpointsApiExplorer()
            .AddCors()
            .AddOpenApi( "api" )
            .AddProblemDetails()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCaching()
            .AddResponseCompression()
            .AddRouting()
            .AddControllersWithViews();

        services.AddWadioUI()
            .AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddDeprecatedApiHeader()
            .AddRadioBrowser( builder => builder.UseHttpHostResolver() )
            .AddTransient<IWadioApi, WadioApi>();

        return services.ConfigureOptions<ConfigureCookiePolicy>()
            .ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureRequestTimeouts>()
            .ConfigureOptions<ConfigureResponseCompression>()
            .ConfigureOptions<ConfigureRouting>()
            .ConfigureOptions<ConfigureScalar>();
    }
}