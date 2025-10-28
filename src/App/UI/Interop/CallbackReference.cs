using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class CallbackReference : IDisposable
{
    private readonly Func<ValueTask> value;
    public DotNetObjectReference<CallbackReference> Reference { get; }

    [DynamicDependency( nameof( Invoke ) )]
    public CallbackReference( Func<ValueTask> value )
    {
        this.value = value;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task Invoke( ) => await value();
}

internal sealed class CallbackReference<T> : IDisposable
{
    private readonly Func<T, ValueTask> value;
    public DotNetObjectReference<CallbackReference<T>> Reference { get; }

    [DynamicDependency( nameof( Invoke ) )]
    public CallbackReference( Func<T, ValueTask> value )
    {
        this.value = value;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task Invoke( T arg ) => await value( arg );
}