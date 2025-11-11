using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class ClipboardInterop( IJSRuntime runtime ) : Interop( runtime, "Clipboard" )
{
    public ValueTask Write( string text, CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeVoidAsync( "write", text ),
        cancellation );
}