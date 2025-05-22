using Microsoft.Extensions.Caching.Memory;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

public abstract class RadioBrowserHostResolver( IMemoryCache cache ) : IDisposable, IRadioBrowserHostResolver
{
    private readonly object cacheKey = new();

    protected IMemoryCache Cache => cache;
    private readonly SemaphoreSlim locker = new( 1, 1 );

    public void Dispose( )
    {
        Dispose( true );
        GC.SuppressFinalize( this );
    }

    protected virtual void Dispose( bool disposing )
    {
        if( disposing )
        {
            locker.Dispose();
            cache.Remove( cacheKey );
        }
    }

    protected abstract ValueTask<RadioBrowserHost> OnResolveHost( CancellationToken cancellation );

    public async ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation )
    {
        if( cache.TryGetValue( cacheKey, out var value ) && value is RadioBrowserHost host )
        {
            return host;
        }

        await locker.WaitAsync( cancellation );
        try
        {
            using var entry = cache.CreateEntry( cacheKey )
                .SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) )
                .SetValue( host = await OnResolveHost( cancellation ) );
        }
        finally
        {
            locker.Release();
        }

        return host;
    }
}