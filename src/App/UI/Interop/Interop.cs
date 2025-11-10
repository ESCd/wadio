using Microsoft.JSInterop;
using Wadio.App.UI.Infrastructure.Imports;

namespace Wadio.App.UI.Interop;

internal abstract class Interop( IJSRuntime runtime, string moduleName ) : IAsyncDisposable
{
    public string ModulePath => $"/Interop/{moduleName}.module.js";

    private readonly SemaphoreSlim semaphore = new( 1, 1 );

    private IJSInProcessObjectReference? module;
    protected IJSRuntime Runtime { get; } = runtime;

    protected async ValueTask Access( Func<IJSInProcessObjectReference, CancellationToken, ValueTask> accessor, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( accessor );

        await EnsureModuleReference( cancellation );
        await accessor( module!, cancellation );
    }

    protected async ValueTask<T> Access<T>( Func<IJSInProcessObjectReference, CancellationToken, ValueTask<T>> accessor, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( accessor );

        await EnsureModuleReference( cancellation );
        return await accessor( module!, cancellation );
    }

    public async ValueTask DisposeAsync( )
    {
        semaphore.Dispose();
        if( module is not null )
        {
            await module.DisposeAsync();
        }

        GC.SuppressFinalize( this );
    }

    private async ValueTask EnsureModuleReference( CancellationToken cancellation )
    {
        PlatformGuard.ThrowIfNotBrowser();
        if( module is not null )
        {
            return;
        }

        await semaphore.WaitAsync( cancellation );
        try
        {
            module ??= await Runtime.InvokeAsync<IJSInProcessObjectReference>( "import", ModulePath );
        }
        finally
        {
            semaphore.Release();
        }
    }
}