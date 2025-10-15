using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DOMInterop( IJSRuntime runtime ) : Interop( runtime, "DOM" )
{
    public ValueTask<OnAppInstalledReference> AddAppInstalledListener( Func<Task> onAppInstalled, CancellationToken cancellation = default ) => Access<OnAppInstalledReference>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onAppInstalled );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addAppInstalledListener",
                cancellation,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<OnClickOutReference> AddClickOutListener( ElementReference element, CancellationToken cancellation = default )
    {
        var reference = await Access(
            ( module, cancellation ) => module.InvokeAsync<IJSObjectReference>( "addClickOutListener", cancellation, element ),
            cancellation );

        return new( reference );
    }

    public ValueTask<OnFullscreenChangeReference> AddFullscreenChangeListener( Func<Task> onFullscreenChange, CancellationToken cancellation = default ) => Access<OnFullscreenChangeReference>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onFullscreenChange );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addFullscreenChangeListener",
                cancellation,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public ValueTask<bool> IsApplicationInstalled( CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeAsync<bool>( "isApplicationInstalled", cancellation ),
        cancellation );

    public ValueTask<bool> IsFullscreen( CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeAsync<bool>( "isFullscreen", cancellation ),
        cancellation );
}

[EventHandler( "onclickout", typeof( MouseEventArgs ) )]
public static partial class EventHandlers;

internal sealed record DOMRect( double Width, double Height, double X, double Y );

internal sealed class OnAppInstalledReference( IJSObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}

internal sealed class OnClickOutReference( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }
}

internal sealed class OnFullscreenChangeReference( IJSObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}