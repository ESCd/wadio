using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class LocalStorageInterop( IJSRuntime runtime ) : Interop( runtime, "LocalStorage" )
{
    public ValueTask<T?> Get<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( string key, CancellationToken cancellation = default )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( key );

        return Access(
            ( module, cancellation ) => module.InvokeAsync<T?>( "get", cancellation, key ),
            cancellation );
    }

    public ValueTask Set<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( string key, T value, CancellationToken cancellation = default )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( key );
        ArgumentNullException.ThrowIfNull( value );

        return Access(
            ( module, cancellation ) => module.InvokeVoidAsync( "set", cancellation, key, value ),
            cancellation );
    }
}