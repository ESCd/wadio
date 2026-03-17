using Octokit;
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

        builder.Services.AddEndpointsApiExplorer()
            .AddCors()
            .AddOpenApi( "api" )
            .AddProblemDetails()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCaching()
            .AddResponseCompression()
            .AddRouting();

        builder.Services.AddDeprecatedApiHeader()
            .AddRadioBrowser( builder => builder.UseHttpHostResolver() )
            .AddTransient<IWadioApi, WadioApi>();

        builder.Services.AddHostedService<MetadataHubWorker>()
            .AddIcecastClient()
            .AddSingleton<IMetadataWorkerContext, MetadataWorkerContext>();

        var signalr = builder.Services.AddSignalR();
        if( builder.Configuration.GetValue<string>( "Azure:SignalR:ConnectionString" ) is not null )
        {
            signalr.AddAzureSignalR();
            builder.Services.ConfigureOptions<ConfigureAzureSignalR>();
        }

        builder.Services.AddTransient<IGitHubClient>( _ => new GitHubClient(
            new ProductHeaderValue(
                "Wadio.Platform.Api",
                WadioVersion.Current ) ) );

        builder.Services.ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureHubs>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureProblemDetails>()
            .ConfigureOptions<ConfigureScalar>();

        return builder;
    }
}