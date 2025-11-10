using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DialogInterop( IJSRuntime runtime )
{
    public ValueTask Close( ElementReference element, CancellationToken cancellation = default )
    {
        Invoke( runtime, "HTMLDialogElement.prototype.close.call", element );
        return ValueTask.CompletedTask;
    }

    public ValueTask ShowModal( ElementReference element, CancellationToken cancellation = default )
    {
        Invoke( runtime, "HTMLDialogElement.prototype.showModal.call", element );
        return ValueTask.CompletedTask;
    }

    private static void Invoke( IJSRuntime runtime, string methood, ElementReference element )
    {
        var js = ( IJSInProcessRuntime )runtime;
        js.InvokeVoid( methood, element );
    }
}