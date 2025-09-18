using Azure.Monitor.OpenTelemetry.AspNetCore;
using ESCd.Extensions.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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
    public static TBuilder WithWadioWeb<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.ConfigureOpenTelemetry();

        builder.Services.AddAsyncCache()
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

        builder.Services.AddWadioUI()
            .AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddDeprecatedApiHeader()
            .AddRadioBrowser( builder => builder.UsePingHostResolver().UseHttpHostResolver() )
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

        builder.Services.AddHealthChecks()
            .AddCheck( "self", ( ) => HealthCheckResult.Healthy(), [ "live" ] );

        builder.Services.ConfigureOptions<ConfigureCookiePolicy>()
            .ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureRequestTimeouts>()
            .ConfigureOptions<ConfigureResponseCompression>()
            .ConfigureOptions<ConfigureRouting>()
            .ConfigureOptions<ConfigureScalar>();

        return builder;
    }

    private static TBuilder ConfigureOpenTelemetry<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Logging.AddOpenTelemetry( logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        } );

        builder.Services.AddOpenTelemetry()
            .WithMetrics( metrics => metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() )
            .WithTracing( tracing => tracing.AddSource( builder.Environment.ApplicationName )
                .AddAspNetCoreInstrumentation( tracing =>
                    tracing.Filter = context =>
                        // NOTE: exclude health check requests from tracing
                        !context.Request.Path.StartsWithSegments( "/healthz" ) && !context.Request.Path.StartsWithSegments( "/alivez" ) )
                .AddHttpClientInstrumentation() );

        return AddOpenTelemetryExporters( builder );

        static TBuilder AddOpenTelemetryExporters( TBuilder builder )
        {
            if( !string.IsNullOrWhiteSpace( builder.Configuration[ "OTEL_EXPORTER_OTLP_ENDPOINT" ] ) )
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            if( !string.IsNullOrEmpty( builder.Configuration[ "APPLICATIONINSIGHTS_CONNECTION_STRING" ] ) )
            {
                builder.Services.AddOpenTelemetry().UseAzureMonitor();
            }

            return builder;
        }
    }
}