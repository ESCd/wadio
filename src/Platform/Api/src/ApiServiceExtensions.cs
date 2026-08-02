using System.Net;
using System.Net.Http.Headers;
using Octokit;
using StackExchange.Redis;
using Wadio.Extensions.CloudflareApi;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.RadioBrowser;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Configuration;
using Wadio.Platform.Api.Hubs;
using Wadio.Platform.Api.Infrastructure;
using Wadio.Platform.Hosting.Configuration;
using Wadio.Platform.Sampler.Client;

namespace Wadio.Platform.Api;

internal static class ApiServiceExtensions
{
    public static TBuilder WithWadioApi<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.AddRedisClientBuilder( "backplane" )
            .WithDistributedCache();

        builder.Services.AddEndpointsApiExplorer()
            .AddCors()
            .AddOpenApi( "api" )
            .AddHttpContextAccessor()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCaching()
            .AddResponseCompression()
            .AddRouting();

        builder.Services.AddCloudflareImagesApi()
            .AddDeprecatedApiHeader()
            .AddRadioBrowser(
                builder => builder.UsePingHostResolver()
                    .UseHttpHostResolver() )
            .AddTransient<IWadioApi, WadioApi>()
            .AddHybridCache();

        builder.Services.AddHostedService<EnforceThumbnailQuotas>()
            .AddHttpClient<StationIconLoader>( http =>
            {
                http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
                http.DefaultRequestVersion = HttpVersion.Version30;

                static ProductInfoHeaderValue UserAgent( )
                {
                    var version = typeof( StationIconLoader ).Assembly.GetName().Version!;
                    return new( "Wadio.Platform.Api.StationIconLoader", version.ToString() );
                }
            } ).ConfigurePrimaryHttpMessageHandler( _ => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectTimeout = TimeSpan.FromMinutes( 60 ),
                UseCookies = false,
                UseProxy = false,
            } ).AddStandardResilienceHandler( options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds( 90 );
                options.Retry.UseJitter = true;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes( 5 );

                options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
            } );

        builder.Services.AddHostedService<MetadataHubWorker>()
            .AddIcecastClient()
            .AddMetadataSampler()
            .AddSingleton<IMetadataWorkerContext, MetadataWorkerContext>();

        builder.Services.AddSignalR()
            .AddStackExchangeRedis();

        builder.Services.AddTransient<IGitHubClient>( _ => new GitHubClient(
            new Octokit.ProductHeaderValue(
                "Wadio.Platform.Api",
                WadioVersion.Current ) ) );

        builder.Services.AddHealthChecks()
            .AddRedis( services => services.GetRequiredService<IConnectionMultiplexer>() );

        builder.Services.ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureHubs>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureProblemDetails>()
            .ConfigureOptions<ConfigureRedisCache>()
            .ConfigureOptions<ConfigureRedisSignalR>()
            .ConfigureOptions<ConfigureScalar>();

        return builder;
    }
}