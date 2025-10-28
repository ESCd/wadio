using Microsoft.AspNetCore.Connections;

namespace Wadio.App.Web.Infrastructure;

internal static class ExceptionExtensions
{
    public static bool IsCancellation( this Exception exception )
    {
        ArgumentNullException.ThrowIfNull( exception );

        if( exception switch
        {
            OperationCanceledException => true,
            ConnectionResetException => true,
            IOException io => io.Message is "The client reset the request stream.",

            _ => false
        } )
        {
            return true;
        }

        return exception.InnerException?.IsCancellation() is true;
    }
}