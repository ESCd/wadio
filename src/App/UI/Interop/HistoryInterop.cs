using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class HistoryInterop( IJSRuntime runtime ) : Interop( runtime, "History" )
{
    public ValueTask Back( CancellationToken cancellation = default ) => Access( ( module, cancellation ) => module.InvokeVoidAsync( "back", cancellation ), cancellation );

    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( CanNavigateData ) )]
    public ValueTask<(bool Backward, bool Forward)> CanNavigate( CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var (backward, forward) = module.Invoke<CanNavigateData>( "canNavigate" );
#pragma warning restore IL2026

        return ValueTask.FromResult( (backward, forward) );
    }, cancellation );

    public ValueTask Forward( CancellationToken cancellation = default ) => Access( ( module, cancellation ) => module.InvokeVoidAsync( "forward", cancellation ), cancellation );

    public ValueTask<bool> IsNavigationApiSupported( CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var supported = module.Invoke<bool>( "isNavigationApiSupported" );
#pragma warning restore IL2026

        return ValueTask.FromResult( supported );
    }, cancellation );
}

sealed file record CanNavigateData( bool Backward, bool Forward );