using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class QuickSearchInterop( IJSRuntime runtime ) : Interop( runtime, "QuickSearch" )
{
    public async ValueTask<IAsyncDisposable> UseKeyboardNavigation( ElementReference container, ElementReference listing, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var reference = module.Invoke<IJSInProcessObjectReference>(
            "useKeyboardNavigation",
            container,
            listing );
#pragma warning restore IL2026

        return ValueTask.FromResult<KeyboardNavigationReference>( new( reference ) );
    }, cancellation );
}

internal sealed class KeyboardNavigationReference( IJSInProcessObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }
    }
}