using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

public abstract class RadioBrowserHostResolver( IAsyncCache cache ) : IDisposable, IRadioBrowserHostResolver
{
    private readonly CacheKey cacheKey = new( nameof( RadioBrowserHostResolver ), Guid.NewGuid().ToString(), "Host" );

    protected IAsyncCache Cache => cache;

    public void Dispose( )
    {
        Dispose( true );
        GC.SuppressFinalize( this );
    }

    protected virtual void Dispose( bool disposing )
    {
        if( disposing )
        {
            cache.Remove( cacheKey );
        }
    }

    protected abstract ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation );

    public async ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation ) => await cache.GetOrCreateAsync( cacheKey, async ( entry, cancellation ) =>
    {
        ArgumentNullException.ThrowIfNull( entry );

        entry.SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
            .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) );

        return await OnResolveHost( cancellation );
    }, cancellation ) ?? throw new HostResolutionException( this );
}

public sealed class HostResolutionException( IRadioBrowserHostResolver resolver ) : InvalidOperationException( $"A {nameof( RadioBrowserHost )} could not be resolved by '{resolver.GetType().FullName}'." )
{
    public IRadioBrowserHostResolver Resolver { get; init; } = resolver;
}