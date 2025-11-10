using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class LocalStorageInterop( IJSRuntime runtime ) : Interop( runtime, "LocalStorage" )
{
    public ValueTask<T?> Get<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( string key, CancellationToken cancellation = default )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( key );

        return Access( ( module, cancellation ) =>
        {
#pragma warning disable IL2026
            var value = module.Invoke<T?>( "get", key );
#pragma warning restore IL2026

            return ValueTask.FromResult( value );
        }, cancellation );
    }

    public ValueTask Set<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( string key, T value, CancellationToken cancellation = default )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( key );
        ArgumentNullException.ThrowIfNull( value );

        return Access( ( module, cancellation ) =>
        {
            module.InvokeVoid( "set", key, value );
            return ValueTask.CompletedTask;
        }, cancellation );
    }
}