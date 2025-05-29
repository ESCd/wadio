using System.Net.Http.Headers;
using ESCd.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Wadio.Extensions.Caching;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Infrastructure;

namespace Wadio.Extensions.RadioBrowser;

public static class RadioBrowserServiceExtensions
{
    public static IServiceCollection AddRadioBrowser( this IServiceCollection services, Action<RadioBrowserBuilder> configure )
    {
        ArgumentNullException.ThrowIfNull( services );
        ArgumentNullException.ThrowIfNull( configure );

        var builder = new RadioBrowserBuilder( services );
        configure( builder );

        return services.AddAsyncCache()
            .AddQueryStringBuilderObjectPool();
    }
}

public sealed class RadioBrowserBuilder
{
    public IHttpClientBuilder Http { get; }
    public IServiceCollection Services { get; }

    public RadioBrowserBuilder( IServiceCollection services )
    {
        Http = services.AddHttpClient<IRadioBrowserClient, RadioBrowserClient>( http =>
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
            .AddTransientHttpErrorPolicy( ConfigureHttpPolicy );

        Http.Services.AddScoped<RadioBrowserHostHandler>();
        Services = services;
    }

    public RadioBrowserBuilder UseHttpHostResolver( Action<HttpHostResolverOptions>? configure = default )
    {
        var options = Services.AddOptions<HttpHostResolverOptions>()
            .BindConfiguration( "RadioBrowser:HttpHostResolver" );

        if( configure is not null )
        {
            options.Configure( configure );
        }

        var factoryKey = typeof( HttpHostResolver ).FullName!;
        Services.AddSingleton<IRadioBrowserHostResolver>( serviceProvider => ActivatorUtilities.CreateInstance<HttpHostResolver>(
            serviceProvider,
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient( factoryKey ) ) )

            .AddHttpClient( factoryKey, http =>
            {
                http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
                http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                static ProductInfoHeaderValue UserAgent( )
                {
                    var version = typeof( RadioBrowserClient ).Assembly.GetName().Version!;
                    return new( "Wadio.HostResolver", version.ToString() );
                }
            } )
            .AddPolicyHandler( Policy.TimeoutAsync<HttpResponseMessage>( TimeSpan.FromSeconds( 2.5 ) ) )
            .AddTransientHttpErrorPolicy( ConfigureHttpPolicy );

        return this;
    }

    public RadioBrowserBuilder UsePingHostResolver( )
    {
        Services.AddSingleton<IRadioBrowserHostResolver, PingHostResolver>();
        return this;
    }

    internal static IAsyncPolicy<HttpResponseMessage> ConfigureHttpPolicy( PolicyBuilder<HttpResponseMessage> policy ) => policy.WaitAndRetryAsync(
        3,
        attempt => TimeSpan.FromSeconds( Math.Pow( 2, attempt ) ) + TimeSpan.FromMilliseconds( Random.Shared.Next( 0, 1000 ) ) );
}