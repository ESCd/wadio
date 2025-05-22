using System.Net.Http.Headers;
using ESCd.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Infrastructure;

namespace Wadio.Extensions.RadioBrowser;

public static class RadioBrowserServiceExtensions
{
    public static IServiceCollection AddRadioBrowser( this IServiceCollection services, Action<IHttpClientBuilder> configure )
    {
        ArgumentNullException.ThrowIfNull( services );
        ArgumentNullException.ThrowIfNull( configure );

        var builder = services.AddTransient<RadioBrowserHostHandler>()
            .AddHttpClient<IRadioBrowserClient, RadioBrowserClient>( http =>
            {
                http.BaseAddress = new( RadioBrowserHostHandler.Authority + "/json/" );
                http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
                http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                static ProductInfoHeaderValue UserAgent( )
                {
                    var version = typeof( RadioBrowserClient ).Assembly.GetName().Version!;
                    return new( "Wadio", version.ToString() );
                }
            } )
            .AddHttpMessageHandler<RadioBrowserHostHandler>()
            .AddTransientHttpErrorPolicy( RadioBrowserHttpBuilderExtensions.ConfigureHttpPolicy );

        configure( builder );
        return services.AddMemoryCache()
            .AddQueryStringBuilderObjectPool();
    }
}

public static class RadioBrowserHttpBuilderExtensions
{
    public static IHttpClientBuilder UseHttpHostResolver( this IHttpClientBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        var key = typeof( HttpHostResolver ).FullName!;

        builder.Services.AddHttpClient( key )
            .AddPolicyHandler( Policy.TimeoutAsync<HttpResponseMessage>( TimeSpan.FromSeconds( 2.5 ) ) )
            .AddTransientHttpErrorPolicy( ConfigureHttpPolicy );

        builder.Services.AddSingleton<IRadioBrowserHostResolver>( serviceProvider =>
        {
            var http = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient( key );
            return ActivatorUtilities.CreateInstance<HttpHostResolver>( serviceProvider, http );
        } );

        return builder;
    }

    public static IHttpClientBuilder UsePingHostResolver( this IHttpClientBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Services.AddSingleton<IRadioBrowserHostResolver, PingHostResolver>();
        return builder;
    }

    internal static IAsyncPolicy<HttpResponseMessage> ConfigureHttpPolicy( PolicyBuilder<HttpResponseMessage> policy ) => policy.WaitAndRetryAsync(
        3,
        attempt => TimeSpan.FromSeconds( Math.Pow( 2, attempt ) ) + TimeSpan.FromMilliseconds( Random.Shared.Next( 0, 1000 ) ) );
}