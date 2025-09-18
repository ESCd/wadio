using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

public abstract class RadioBrowserHostResolver( IAsyncCache cache ) : IAsyncDisposable, IRadioBrowserHostResolver
{
    private readonly CacheKey cacheKey = new( nameof( RadioBrowserHostResolver ), Guid.NewGuid().ToString(), "Host" );

    protected IAsyncCache Cache => cache;

    public virtual async ValueTask DisposeAsync( )
    {
        await cache.RemoveAsync( cacheKey );
        GC.SuppressFinalize( this );
    }

    protected abstract ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation );

    public async ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation ) => await cache.GetOrCreateAsync( cacheKey, async ( entry, cancellation ) =>
    {
        ArgumentNullException.ThrowIfNull( entry );

        var host = await OnResolveHost( cancellation ).ConfigureAwait( false );
        if( host is null )
        {
            entry.SetAbsoluteExpiration( TimeSpan.FromHours( 30 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 5 ) );

            return default;
        }

        entry.SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
            .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) );

        return host;
    }, cancellation ).ConfigureAwait( false ) ?? throw new HostResolutionException( this );
}

public sealed class HostResolutionException( IRadioBrowserHostResolver resolver ) : InvalidOperationException( $"A {nameof( RadioBrowserHost )} could not be resolved by '{resolver.GetType().FullName}'." )
{
    public IRadioBrowserHostResolver Resolver { get; init; } = resolver;
}