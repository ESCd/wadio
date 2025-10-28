using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Wadio.App.Web.Infrastructure;

public static class RequestCancellationMiddleware
{
    public static IApplicationBuilder UseRequestCancellation( this IApplicationBuilder app )
    {
        ArgumentNullException.ThrowIfNull( app );

        return app.Use( async ( context, next ) =>
        {
            try
            {
                await next();
            }
            catch( Exception e ) when( e.IsCancellation() && !context.Response.HasStarted )
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

                var endpoint = context.GetEndpoint();
                if( endpoint?.IsApiEndpoint() is true )
                {
                    await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync( new()
                    {
                        AdditionalMetadata = endpoint.Metadata,
                        Exception = e,
                        HttpContext = context,
                        ProblemDetails = context.RequestServices.GetRequiredService<ProblemDetailsFactory>().CreateProblemDetails( context, context.Response.StatusCode )
                    } );
                }
            }
        } );
    }
}