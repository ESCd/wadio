using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DOMInterop( IJSRuntime runtime ) : Interop( runtime, "DOM" )
{
    public async ValueTask<IAsyncDisposable> AddAppInstalledListener( Func<ValueTask> onAppInstalled, CancellationToken cancellation = default ) => await Access<OnAppInstalledReference>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onAppInstalled );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addAppInstalledListener",
                cancellation,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddBreakpointListener( Func<BreakpointChangeEventArgs, ValueTask> onBreakpointChange, CancellationToken cancellation = default ) => await Access<OnBreakpointListener>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference<BreakpointChangeEventArgs>( onBreakpointChange );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addBreakpointListener",
                cancellation,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddClickOutListener( ElementReference element, CancellationToken cancellation = default ) => await Access<OnClickOutListener>( async ( module, cancellation ) =>
    {
        var reference = await module.InvokeAsync<IJSObjectReference>(
            "addClickOutListener",
            cancellation,
            element );

        return new( reference );

    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddFullscreenChangeListener( Func<ValueTask> onFullscreenChange, CancellationToken cancellation = default ) => await Access<OnFullscreenChangeListener>( async ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onFullscreenChange );
        try
        {
            var reference = await module.InvokeAsync<IJSObjectReference>(
                "addFullscreenChangeListener",
                cancellation,
                callback.Reference );

            return new( reference, callback );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

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

    public async ValueTask<IAsyncDisposable> AddResizeObserver( ElementReference element, CancellationToken cancellation = default ) => await Access<ResizeObserverReference>( async ( module, cancellation ) =>
    {
        var reference = await module.InvokeAsync<IJSObjectReference>(
            "addResizeObserver",
            cancellation,
            element );

        return new( reference );
    }, cancellation );

    public ValueTask<DOMBreakpoint> GetActiveBreakpoint( CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeAsync<DOMBreakpoint>( "getActiveBreakpoint", cancellation ),
        cancellation );

    public ValueTask<bool> IsApplicationInstalled( CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeAsync<bool>( "isApplicationInstalled", cancellation ),
        cancellation );

    public ValueTask<bool> IsFullscreen( CancellationToken cancellation = default ) => Access(
        ( module, cancellation ) => module.InvokeAsync<bool>( "isFullscreen", cancellation ),
        cancellation );
}

// [EventHandler( "onbreakpointchange", typeof( BreakpointChangeEventArgs ) )]
[EventHandler( "onclickout", typeof( MouseEventArgs ) )]
[EventHandler( "onresize", typeof( ResizeEventArgs ) )]
[EventHandler( "onresizedebounce", typeof( ResizeEventArgs ) )]
public static partial class EventHandlers;

internal sealed class BreakpointChangeEventArgs : EventArgs
{
    public DOMBreakpoint From { get; init; }
    public DOMBreakpoint To { get; init; }
}

public sealed class ResizeEventArgs : EventArgs
{
    public double Height { get; init; }
    public double Width { get; init; }
}

internal enum DOMBreakpoint
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
    ExtraExtraLarge,
}

internal sealed record DOMRect( double Width, double Height, double X, double Y );

sealed file class OnAppInstalledReference( IJSObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}

sealed file class OnBreakpointListener( IJSObjectReference reference, CallbackReference<BreakpointChangeEventArgs> callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}

sealed file class OnClickOutListener( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }
}

sealed file class OnFullscreenChangeListener( IJSObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}

sealed file class OnHotKeyListener( IJSObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }
}

sealed file class ResizeObserverReference( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }
}