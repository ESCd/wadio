using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class CallbackReference : IDisposable
{
    private readonly Func<Task> value;
    public DotNetObjectReference<CallbackReference> Reference { get; }

    [DynamicDependency( nameof( Invoke ) )]
    public CallbackReference( Func<Task> value )
    {
        this.value = value;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public Task Invoke( ) => value();
}

internal sealed class CallbackReference<T> : IDisposable
{
    private readonly Func<T, Task> value;
    public DotNetObjectReference<CallbackReference<T>> Reference { get; }

    [DynamicDependency( nameof( Invoke ) )]
    public CallbackReference( Func<T, Task> value )
    {
        this.value = value;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public Task Invoke( T arg ) => value( arg );
}