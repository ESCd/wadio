using System.Net.Http.Headers;
using ESCd.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Infrastructure;

namespace Wadio.Extensions.RadioBrowser;

public static class RadioBrowserServiceExtensions
{
    public static IServiceCollection AddRadioBrowser( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddScoped<RadioBrowserHostHandler>()
            .AddHttpClient<IRadioBrowserClient, RadioBrowserClient>( http =>
            {
                http.BaseAddress = new( RadioBrowserHostHandler.Authority );
                http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
                http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                static ProductInfoHeaderValue UserAgent( )
                {
                    var version = typeof( RadioBrowserClient ).Assembly.GetName().Version!;
                    return new( "Wadio", version.ToString() );
                }
            } )
            .AddHttpMessageHandler<RadioBrowserHostHandler>()
            .AddTransientHttpErrorPolicy( ConfigureHttpPolicy );

        return services.AddMemoryCache()
            .AddQueryStringBuilderObjectPool();

        static IAsyncPolicy<HttpResponseMessage> ConfigureHttpPolicy( PolicyBuilder<HttpResponseMessage> policy ) => policy.WaitAndRetryAsync(
            3,
            attempt => TimeSpan.FromSeconds( Math.Pow( 2, attempt ) ) + TimeSpan.FromMilliseconds( Random.Shared.Next( 0, 1000 ) ) );
    }
}