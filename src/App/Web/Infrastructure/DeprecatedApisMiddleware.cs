using Microsoft.Extensions.Options;

namespace Wadio.App.Web.Infrastructure;

public static class DeprecatedApisMiddleware
{
    public static IServiceCollection AddDeprecatedApiHeader( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddOptions<DeprecatedApiHeaderOptions>()
            .Validate( options => !string.IsNullOrWhiteSpace( options.HeaderName ), $"{nameof( DeprecatedApiHeaderOptions.HeaderName )} is required." );

        return services;
    }

    public static IApplicationBuilder UseDeprecatedApiHeader( this IApplicationBuilder app )
    {
        ArgumentNullException.ThrowIfNull( app );

        var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<DeprecatedApiHeaderOptions>>();
        return app.Use( ( context, next ) =>
        {
            context.Response.OnStarting( ( ) =>
            {
                var endpoint = context.GetEndpoint();
                if( endpoint?.IsApiEndpoint() is true )
                {
                    var deprecation = endpoint.Metadata.GetMetadata<ObsoleteAttribute>();
                    if( deprecation is not null )
                    {
                        context.Response.Headers.Append( options.CurrentValue.HeaderName, deprecation.Message ?? "true" );
                    }
                }

                return Task.CompletedTask;
            } );

            return next();
        } );
    }
}

public sealed class DeprecatedApiHeaderOptions
{
    public string HeaderName { get; set; } = "X-Api-Deprecated";
}