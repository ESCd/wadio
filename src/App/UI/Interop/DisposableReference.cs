using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal class DisposableReference( IJSObjectReference reference ) : IAsyncDisposable
{
    protected IJSObjectReference Value => reference;

    public async ValueTask DisposeAsync( )
    {
        try
        {
            OnDispose( reference );
            await OnDisposeAsync( reference );

            await reference.InvokeVoidAsync( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }
    }

    protected virtual void OnDispose( IJSObjectReference reference ) { }
    protected virtual ValueTask OnDisposeAsync( IJSObjectReference reference ) => default;
}