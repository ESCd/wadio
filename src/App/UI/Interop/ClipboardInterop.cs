using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class ClipboardInterop( IJSRuntime runtime )
{
    public ValueTask Write( string text, CancellationToken cancellation = default )
        => runtime.InvokeVoidAsync( "navigator.clipboard.writeText", cancellation, text );
}