using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Wadio.Extensions.Caching.Abstractions;

public sealed class CacheEntryBuilder( CacheKey key ) : ICacheEntry
{
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public IList<IChangeToken> ExpirationTokens { get; } = [];
    public CacheKey Key { get; } = key;
    object ICacheEntry.Key => Key;
    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = [];
    public CacheItemPriority Priority { get; set; }
    public long? Size { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public object? Value { get; set; }

    public void Dispose( )
    {
    }

    public MemoryCacheEntryOptions ToOptions( )
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = AbsoluteExpiration,
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
            Priority = Priority,
            Size = Size,
            SlidingExpiration = SlidingExpiration,
        };

        foreach( var token in ExpirationTokens )
        {
            options.ExpirationTokens.Add( token );
        }

        foreach( var callback in PostEvictionCallbacks )
        {
            options.PostEvictionCallbacks.Add( callback );
        }

        return options;
    }
}