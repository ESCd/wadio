using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class KeyboardInterop( IJSRuntime runtime ) : Interop( runtime, "Keyboard" )
{
    public ValueTask<IAsyncDisposable> AddHotKeyListener( string hotkey, Func<ValueTask> onHotKey, CancellationToken cancellation = default ) => AddHotKeyListener( hotkey, default, onHotKey, cancellation );

    public async ValueTask<IAsyncDisposable> AddHotKeyListener( string hotkey, string? scope, Func<ValueTask> onHotKey, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onHotKey );
        try
        {
#pragma warning disable IL2026
            var reference = module.Invoke<IJSInProcessObjectReference>(
                "addHotKeyListener",
                hotkey,
                scope,
                callback.Reference );
#pragma warning restore IL2026

            return ValueTask.FromResult<OnHotKeyListener>( new( reference, callback ) );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );
}

sealed file class OnHotKeyListener( IJSInProcessObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }

        callback.Dispose();
    }
}