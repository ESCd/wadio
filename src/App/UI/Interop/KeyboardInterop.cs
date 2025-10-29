using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class KeyboardInterop( IJSRuntime runtime ) : Interop( runtime, "Keyboard" )
{
    public ValueTask<IAsyncDisposable> AddHotKeyListener( string hotkey, Func<ValueTask> onHotKey, CancellationToken cancellation = default ) => AddHotKeyListener( hotkey, default, onHotKey, cancellation );

    public async ValueTask<IAsyncDisposable> AddHotKeyListener( string hotkey, string? scope, Func<ValueTask> onHotKey, CancellationToken cancellation = default ) => await Access<OnHotKeyListener>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onHotKey );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addHotKeyListener",
                cancellation,
                hotkey,
                scope,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );
}

sealed file class OnHotKeyListener( IJSObjectReference reference, CallbackReference callback ) : DisposableReference( reference )
{
    protected override void OnDispose( IJSObjectReference reference ) => callback.Dispose();
}