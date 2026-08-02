using System.Net;
using ESCd.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Client.Infrastructure;

namespace Wadio.Platform.Api.Client;

public static class WadioApiClientServiceExtensions
{
    public static IServiceCollection AddWadioApiClient( this IServiceCollection services, Action<IHttpClientBuilder>? configure = default )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddQueryStringBuilderObjectPool()
            .TryAddSingleton( serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().CreateStringBuilderPool() );

        var builder = services.AddTransient<ApiDeprecationHandler>()
            .AddHttpClient<IWadioApi, WadioApi>( ConfigureDefaults )
            .AddHttpMessageHandler<ApiDeprecationHandler>()
            .AddHttpMessageHandler( ( ) => new ApiProblemHandler() );

        builder.AddStandardResilienceHandler( options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds( 30 );
            options.Retry.UseJitter = true;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes( 2.5 );

            options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
        } );

        configure?.Invoke( builder );
        return services;

        static void ConfigureDefaults( HttpClient http )
        {
            ArgumentNullException.ThrowIfNull( http );

            http.DefaultRequestHeaders.UserAgent.Add( new( "Wadio.Platform.Api.Client", WadioVersion.Current ) );
            http.DefaultRequestVersion = HttpVersion.Version30;
        }
    }
}