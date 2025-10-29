using Microsoft.JSInterop;
using Wadio.App.UI.Infrastructure.Imports;

namespace Wadio.App.UI.Interop;

internal abstract class Interop( IJSRuntime runtime, string moduleName ) : IAsyncDisposable
{
    public string ModulePath => $"/Interop/{moduleName}.module.js";

    private readonly SemaphoreSlim semaphore = new( 1, 1 );

    private IJSObjectReference? module;
    protected IJSRuntime Runtime { get; } = runtime;

    protected async ValueTask Access( Func<IJSObjectReference, CancellationToken, ValueTask> method, CancellationToken cancellation )
    {
        await EnsureModuleReference( cancellation );
        await method( module!, cancellation );
    }

    protected async ValueTask<T> Access<T>( Func<IJSObjectReference, CancellationToken, ValueTask<T>> method, CancellationToken cancellation )
    {
        await EnsureModuleReference( cancellation );
        return await method( module!, cancellation );
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
            module ??= await Runtime.InvokeAsync<IJSObjectReference>( "import", ModulePath );
        }
        finally
        {
            semaphore.Release();
        }
    }
}