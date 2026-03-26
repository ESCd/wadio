using Octokit;
using StackExchange.Redis;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.RadioBrowser;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Configuration;
using Wadio.Platform.Api.Hubs;
using Wadio.Platform.Api.Infrastructure;
using Wadio.Platform.Hosting.Configuration;

namespace Wadio.Platform.Api;

internal static class ApiServiceExtensions
{
    public static TBuilder WithWadioApi<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.AddRedisClient( "backplane" );

        builder.Services.AddEndpointsApiExplorer()
            .AddCors()
            .AddOpenApi( "api" )
            .AddHttpContextAccessor()
            .AddProblemDetails()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCaching()
            .AddResponseCompression()
            .AddRouting();

        builder.Services.AddDeprecatedApiHeader()
            .AddRadioBrowser(
                builder => builder.UsePingHostResolver()
                    .UseHttpHostResolver() )
            .AddTransient<IWadioApi, WadioApi>();

        builder.Services.AddHostedService<MetadataHubWorker>()
            .AddIcecastClient()
            .AddSingleton<IMetadataWorkerContext, MetadataWorkerContext>();

        builder.Services.AddSignalR()
            .AddStackExchangeRedis();

        builder.Services.AddTransient<IGitHubClient>( _ => new GitHubClient(
            new ProductHeaderValue(
                "Wadio.Platform.Api",
                WadioVersion.Current ) ) );

        builder.Services.AddHealthChecks()
            .AddRedis( services => services.GetRequiredService<IConnectionMultiplexer>() );

        builder.Services.ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureHubs>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureProblemDetails>()
            .ConfigureOptions<ConfigureRedisSignalR>()
            .ConfigureOptions<ConfigureScalar>();

        return builder;
    }
}