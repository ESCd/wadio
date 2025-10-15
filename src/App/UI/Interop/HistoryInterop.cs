using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class HistoryInterop( IJSRuntime runtime ) : Interop( runtime, "History" )
{
    public ValueTask Back( CancellationToken cancellation = default ) => Access( ( module, cancellation ) => module.InvokeVoidAsync( "back", cancellation ), cancellation );

    public ValueTask<(bool Backward, bool Forward)> CanNavigate( CancellationToken cancellation = default ) => Access( async ( module, cancellation ) =>
    {
        var (backward, forward) = await module.InvokeAsync<CanNavigateData>( "canNavigate", cancellation );
        return (backward, forward);
    }, cancellation );

    public ValueTask Forward( CancellationToken cancellation = default ) => Access( ( module, cancellation ) => module.InvokeVoidAsync( "forward", cancellation ), cancellation );

    public ValueTask<bool> IsNavigationApiSupported( CancellationToken cancellation = default ) => Access( ( module, cancellation ) => module.InvokeAsync<bool>( "isNavigationApiSupported", cancellation ), cancellation );
}

sealed file record CanNavigateData( bool Backward, bool Forward );