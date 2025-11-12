using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DialogInterop( IJSRuntime runtime ) : Interop( runtime, "Dialog" )
{
    public ValueTask Close( ElementReference element, CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
        module.InvokeVoid( "close", element );
        return default;
    }, cancellation );

    public ValueTask ShowModal( ElementReference element, CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
        module.InvokeVoid( "showModal", element );
        return default;
    }, cancellation );
}