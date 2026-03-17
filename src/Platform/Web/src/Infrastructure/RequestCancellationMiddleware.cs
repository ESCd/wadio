using Wadio.Platform.Hosting;

namespace Wadio.Platform.Web.Infrastructure;

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
            }
        } );
    }
}