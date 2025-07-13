using Microsoft.Extensions.Caching.Memory;

namespace Wadio.Extensions.Caching.Abstractions;

public interface IAsyncCache
{
    public ValueTask<T?> GetOrCreateAsync<T>( CacheKey key, Func<CacheEntryBuilder, CancellationToken, ValueTask<T>> factory, CancellationToken cancellation );
    public void Remove( CacheKey key );
    public ValueTask<T> SetAsync<T>( CacheKey key, T value, MemoryCacheEntryOptions options, CancellationToken cancellation );
}