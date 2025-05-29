using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DOMInterop( IJSRuntime runtime ) : Interop( runtime, "DOM" )
{
    public async ValueTask<OnClickOutReference> AddClickOutListener( ElementReference element, CancellationToken cancellation = default )
    {
        var reference = await Access(
            ( module, cancellation ) => module.InvokeAsync<IJSObjectReference>( "addClickOutListener", cancellation, element ),
            cancellation );

        return new( reference );
    }
}

[EventHandler( "onclickout", typeof( MouseEventArgs ) )]
public static partial class EventHandlers;

internal sealed record DOMRect( double Width, double Height, double X, double Y );

internal sealed class OnClickOutReference( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }
}