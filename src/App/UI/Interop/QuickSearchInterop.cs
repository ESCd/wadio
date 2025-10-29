using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class QuickSearchInterop( IJSRuntime runtime ) : Interop( runtime, "QuickSearch" )
{
    public async ValueTask<IAsyncDisposable> UseKeyboardNavigation( ElementReference container, ElementReference listing, CancellationToken cancellation = default ) => await Access<DisposableReference>( async ( module, cancellation ) =>
    {
        var reference = await module.InvokeAsync<IJSObjectReference>(
            "useKeyboardNavigation",
            cancellation,
            container,
            listing );

        return new( reference );
    }, cancellation );
}