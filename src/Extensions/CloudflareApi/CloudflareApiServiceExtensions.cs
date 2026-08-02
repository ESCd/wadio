using System.Net;
using System.Net.Http.Headers;
using ESCd.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wadio.Extensions.CloudflareApi.Abstractions;

namespace Wadio.Extensions.CloudflareApi;

public static class CloudflareApiServiceExtensions
{
    public static IServiceCollection AddCloudflareImagesApi( this IServiceCollection services, Action<CloudflareApiOptions>? configure = default )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddQueryStringBuilderObjectPool()
            .AddTransient<CloudflareImageLink>()
            .AddHttpClient<ICloudflareImagesApi, CloudflareImagesApi>( ( services, http ) =>
            {
                ArgumentNullException.ThrowIfNull( services );
                ArgumentNullException.ThrowIfNull( http );

                var options = services.GetRequiredService<IOptionsMonitor<CloudflareImagesApiOptions>>()
                    .Get( Options.DefaultName );

                http.BaseAddress = new Uri( $"https://api.cloudflare.com/client/v4/accounts/{options.AccountId}/images/" );
                http.DefaultRequestVersion = HttpVersion.Version30;

                http.DefaultRequestHeaders.Authorization = new( "Bearer", options.ApiToken );
                http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );

                static ProductInfoHeaderValue UserAgent( )
                {
                    var version = typeof( CloudflareImagesApi ).Assembly.GetName().Version!;
                    return new( "Wadio.Extensions.CloudflareApi", version.ToString() );
                }
            } )
            .AddStandardResilienceHandler();

        services.AddOptions<CloudflareImagesApiOptions>()
            .BindConfiguration( "CloudflareApi" )
            .BindConfiguration( "CloudflareApi:Images" )
            .Configure( options => configure?.Invoke( options ) );

        return services;
    }
}

public abstract class CloudflareApiOptions
{
    public string? AccountId { get; init; }
    public string? ApiToken { get; init; }
}

public sealed class CloudflareImagesApiOptions : CloudflareApiOptions
{
    public string? AccountHash { get; init; }
}