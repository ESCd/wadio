using Microsoft.FeatureManagement;
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

        services.AddFeatureManagement();
        services.AddHealthChecks();

        services.AddEndpointsApiExplorer()
            .AddCors()
            .AddMemoryCache()
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
            .AddRadioBrowser()
            .AddTransient<IWadioApi, WadioApi>();

        return services.ConfigureOptions<ConfigureCookiePolicy>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureResponseCompression>()
            .ConfigureOptions<ConfigureRouting>()
            .ConfigureOptions<ConfigureScalar>();
    }
}