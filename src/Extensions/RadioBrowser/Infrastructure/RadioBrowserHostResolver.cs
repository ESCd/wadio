using Microsoft.Extensions.Caching.Hybrid;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

public abstract class RadioBrowserHostResolver( HybridCache cache ) : IAsyncDisposable, IRadioBrowserHostResolver
{
    private string CacheKey => $"{GetType().Name}/{Id}/Host";

    protected HybridCache Cache => cache;
    protected Guid Id { get; } = Guid.NewGuid();

    public virtual async ValueTask DisposeAsync( )
    {
        await Cache.RemoveAsync( CacheKey );
        GC.SuppressFinalize( this );
    }

    protected abstract ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation );

    public async ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation ) => await Cache.GetOrCreateAsync(
        CacheKey,
        OnResolveHost,
        new()
        {
            Expiration = TimeSpan.FromHours( 2 ),
        },
        default,
        cancellation ) ?? throw new HostResolutionException( this );
}

public sealed class HostResolutionException( IRadioBrowserHostResolver resolver ) : InvalidOperationException( $"A {nameof( RadioBrowserHost )} could not be resolved by '{resolver.GetType().FullName}'." )
{
    public IRadioBrowserHostResolver Resolver { get; init; } = resolver;
}