using ESCd.Extensions.Caching;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI;
using Wadio.App.Web.Configuration;
using Wadio.App.Web.Hubs;
using Wadio.App.Web.Infrastructure;
using Wadio.Extensions.Icecast;
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
            .AddRadioBrowser( builder => builder.UsePingHostResolver().UseHttpHostResolver() )
            .AddTransient<IWadioApi, WadioApi>();

        services.AddHostedService<MetadataHubWorker>()
            .AddIcecastClient()
            .AddSingleton<IMetadataWorkerContext, MetadataWorkerContext>();

        services.AddSignalR();
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