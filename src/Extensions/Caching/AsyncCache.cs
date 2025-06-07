using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Extensions.Caching.Abstractions;

namespace Wadio.Extensions.Caching;

internal sealed class AsyncCache( IMemoryCache cache ) : IAsyncCache, IDisposable
{
    private bool disposed;
    private readonly ConcurrentDictionary<CacheKey, AsyncCacheLock> locks = [];

    public void Dispose( )
    {
        if( !disposed )
        {
            cache.Dispose();
            foreach( var locker in locks.Values )
            {
                locker.Dispose();
            }

            locks.Clear();
            disposed = true;
        }

        GC.SuppressFinalize( this );
    }

    public T? GetOrCreate<T>( CacheKey key, Func<ICacheEntry, T> factory )
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( factory );
        ObjectDisposedException.ThrowIf( disposed, this );

        return cache.GetOrCreate( key, factory );
    }

    public async ValueTask<T?> GetOrCreateAsync<T>( CacheKey key, Func<CacheEntryBuilder, CancellationToken, ValueTask<T>> factory, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( factory );
        ObjectDisposedException.ThrowIf( disposed, this );

        if( cache.TryGetValue<T>( key, out var value ) )
        {
            return value;
        }

        using( await locks.GetOrAdd( key, _ => new() ).Aquire( cancellation ) )
        {
            if( cache.TryGetValue( key, out value ) )
            {
                return value;
            }

            using var entry = new CacheEntryBuilder( key );

            value = await factory( entry, cancellation );
            return cache.Set( key, value, entry.ToOptions() );
        }
    }

    public void Remove( CacheKey key )
    {
        ArgumentNullException.ThrowIfNull( key );
        ObjectDisposedException.ThrowIf( disposed, this );

        cache.Remove( key );
        if( locks.TryRemove( key, out var locker ) )
        {
            locker.OnRemoved();
        }
    }

    private sealed class AsyncCacheLock : IDisposable
    {
        private readonly SemaphoreSlim locker = new( 1, 1 );

        private int count;

        public int Count => count;
        public bool IsRemoved { get; private set; }

        public async Task<AsyncCacheRelease> Aquire( CancellationToken cancellation )
        {
            Interlocked.Increment( ref count );
            try
            {
                await locker.WaitAsync( cancellation );
                return new( this );
            }
            catch( OperationCanceledException )
            {
                Interlocked.Decrement( ref count );
                throw;
            }
        }

        public void Dispose( ) => locker.Dispose();
        public void OnRemoved( )
        {
            IsRemoved = true;
            if( count is 0 )
            {
                Dispose();
            }
        }

        public void Release( )
        {
            locker.Release();
            if( Interlocked.Decrement( ref count ) is 0 && IsRemoved )
            {
                Dispose();
            }
        }
    }

    private readonly struct AsyncCacheRelease( AsyncCacheLock locker ) : IDisposable
    {
        public void Dispose( ) => locker.Release();
    }
}