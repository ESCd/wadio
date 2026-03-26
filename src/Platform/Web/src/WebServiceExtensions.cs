using Wadio.Platform.Api.Client;
using Wadio.Platform.Web.Configuration;
using Wadio.Platform.Web.UI;

namespace Wadio.Platform.Web;

internal static class WebServiceExtensions
{
    public static TBuilder WithWadioWeb<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Services.AddCors()
            .AddOutputCache()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCaching()
            .AddResponseCompression()
            .AddRouting()
            .AddControllersWithViews();

        builder.Services.AddWadioUI()
            .AddWadioApiClient( api => api.ConfigureHttpClient( http => http.BaseAddress = new( "https+http://api/" ) ) )
            .AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.ConfigureOptions<ConfigureCookiePolicy>();

        return builder;
    }
}