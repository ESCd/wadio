using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class DOMInterop( IJSRuntime runtime ) : Interop( runtime, "DOM" )
{
    public async ValueTask<IAsyncDisposable> AddAppInstalledListener( Func<ValueTask> onAppInstalled, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onAppInstalled );
        try
        {
#pragma warning disable IL2026
            var reference = module.Invoke<IJSInProcessObjectReference>(
                "addAppInstalledListener",
                callback.Reference );
#pragma warning restore IL2026

            return ValueTask.FromResult<OnAppInstalledReference>( new( reference, callback ) );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddBreakpointListener( Func<BreakpointChangeEventArgs, ValueTask> onBreakpointChange, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
        var callback = new CallbackReference<BreakpointChangeEventArgs>( onBreakpointChange );
        try
        {
#pragma warning disable IL2026
            var reference = module.Invoke<IJSInProcessObjectReference>(
                "addBreakpointListener",
                callback.Reference );
#pragma warning restore IL2026

            return ValueTask.FromResult<OnBreakpointListener>( new( reference, callback ) );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddClickOutListener( ElementReference element, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var reference = module.Invoke<IJSInProcessObjectReference>(
            "addClickOutListener",
            element );
#pragma warning restore IL2026

        return ValueTask.FromResult<OnClickOutListener>( new( reference ) );
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddFullscreenChangeListener( Func<ValueTask> onFullscreenChange, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
        var callback = new CallbackReference( onFullscreenChange );
        try
        {
#pragma warning disable IL2026
            var reference = module.Invoke<IJSInProcessObjectReference>(
                "addFullscreenChangeListener",
                callback.Reference );
#pragma warning restore IL2026

            return ValueTask.FromResult<OnFullscreenChangeListener>( new( reference, callback ) );
        }
        catch
        {
            callback.Dispose();
            throw;
        }
    }, cancellation );

    public async ValueTask<IAsyncDisposable> AddResizeObserver( ElementReference element, CancellationToken cancellation = default ) => await Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var reference = module.Invoke<IJSInProcessObjectReference>(
            "addResizeObserver",
            element );
#pragma warning restore IL2026

        return ValueTask.FromResult<ResizeObserverReference>( new( reference ) );
    }, cancellation );

    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( DOMBreakpoint ) )]
    public ValueTask<DOMBreakpoint> GetActiveBreakpoint( CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var breakpoint = module.Invoke<DOMBreakpoint>( "getActiveBreakpoint" );
#pragma warning restore IL2026

        return ValueTask.FromResult( breakpoint );
    }, cancellation );

    public ValueTask<bool> IsApplicationInstalled( CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var installed = module.Invoke<bool>( "isApplicationInstalled" );
#pragma warning restore IL2026

        return ValueTask.FromResult( installed );
    }, cancellation );

    public ValueTask<bool> IsFullscreen( CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var fullscreen = module.Invoke<bool>( "isFullscreen" );
#pragma warning restore IL2026

        return ValueTask.FromResult( fullscreen );
    }, cancellation );
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

sealed file class OnAppInstalledReference( IJSInProcessObjectReference reference, CallbackReference callback ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException )
        {
        }

        callback.Dispose();
    }
}

sealed file class OnBreakpointListener( IJSInProcessObjectReference reference, CallbackReference<BreakpointChangeEventArgs> callback ) : IAsyncDisposable
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

    // protected override void OnDispose( IJSObjectReference reference ) => callback.Dispose();
}

sealed file class OnClickOutListener( IJSInProcessObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }
    }
}

sealed file class OnFullscreenChangeListener( IJSInProcessObjectReference reference, CallbackReference callback ) : IAsyncDisposable
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

sealed file class ResizeObserverReference( IJSInProcessObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }
    }
}