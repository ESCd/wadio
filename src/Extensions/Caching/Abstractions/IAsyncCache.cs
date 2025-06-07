namespace Wadio.Extensions.Caching.Abstractions;

public interface IAsyncCache
{
    public ValueTask<T?> GetOrCreateAsync<T>( CacheKey key, Func<CacheEntryBuilder, CancellationToken, ValueTask<T>> factory, CancellationToken cancellation );
    public void Remove( CacheKey key );
}