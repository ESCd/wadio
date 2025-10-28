using Microsoft.AspNetCore.SignalR;

namespace Wadio.App.Web.Infrastructure;

internal sealed class HubCancellationFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync( HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next )
    {
        ArgumentNullException.ThrowIfNull( context );

        try
        {
            return await next( context );
        }
        catch( Exception exception ) when( context.Context.ConnectionAborted.IsCancellationRequested && exception.IsCancellation() )
        {
            // NOTE: do nothing
            return default;
        }
    }

    public async Task OnConnectedAsync( HubLifetimeContext context, Func<HubLifetimeContext, Task> next )
    {
        ArgumentNullException.ThrowIfNull( context );

        try
        {
            await next( context );
        }
        catch( Exception exception ) when( context.Context.ConnectionAborted.IsCancellationRequested && exception.IsCancellation() )
        {
            // NOTE: do nothing
            return;
        }
    }
}