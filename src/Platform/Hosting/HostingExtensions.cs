using HealthChecks.ApplicationStatus.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Hosting.Abstractions;
using Wadio.Platform.Hosting.Configuration;

namespace Wadio.Platform.Hosting;

public static class HostingExtensions
{
    public static TBuilder WithPlatformDefaults<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.WithPlatformTelemetry();
        builder.WithPlatformHealthChecks();

        builder.Services.AddServiceDiscovery()
            .ConfigureHttpClientDefaults( http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            } )
            .ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureRequestTimeouts>()
            .ConfigureOptions<ConfigureResponseCompression>()
            .ConfigureOptions<ConfigureRouting>();

        builder.Services.AddOptions<PlatformOptions>()
            .BindConfiguration( "Platform" )
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    private static TBuilder WithPlatformTelemetry<TBuilder>( this TBuilder builder, Action<OpenTelemetryBuilder>? configure = default )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Logging.AddOpenTelemetry( logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        } );

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource( resource => resource.AddAttributes( [
                // new( "service.name", nameof(DegenBot) ),
                new( "service.version", WadioVersion.Current.ToString() ) ] ) )
            .WithMetrics( metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddEventCountersInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
            } )
            .WithTracing( tracing =>
            {
                tracing.AddAspNetCoreInstrumentation( tracing =>
                {
                    // NOTE: exclude health check requests from tracing
                    tracing.Filter = context => !context.Request.Path.StartsWithSegments( "/healthz" ) && !context.Request.Path.StartsWithSegments( "/alivez" );
                } );

                tracing.AddHttpClientInstrumentation();
                tracing.AddSource( builder.Environment.ApplicationName );
            } );

        if( !string.IsNullOrWhiteSpace( builder.Configuration[ "OTEL_EXPORTER_OTLP_ENDPOINT" ] ) )
        {
            telemetry.UseOtlpExporter();
        }

        configure?.Invoke( telemetry );
        return builder;
    }

    private static TBuilder WithPlatformHealthChecks<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck( "self", ( ) => HealthCheckResult.Healthy(), [ "live" ] )
            .AddApplicationStatus();

        return builder;
    }

    public static WebApplication MapPlatformEndpoints( this WebApplication app )
    {
        ArgumentNullException.ThrowIfNull( app );

        if( app.Environment.IsDevelopment() )
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks( HealthCheckDefaults.HealthEndpoint )
                .AllowAnonymous()
                .DisableRequestTimeout();

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks( HealthCheckDefaults.AlivenessEndpoint, new() { Predicate = r => r.Tags.Contains( "live" ) } )
                .AllowAnonymous()
                .DisableRequestTimeout();
        }

        return app;
    }
}
