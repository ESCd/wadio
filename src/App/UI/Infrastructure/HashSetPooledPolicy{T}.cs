using Microsoft.Extensions.ObjectPool;

namespace Wadio.App.UI.Infrastructure;

public sealed class HashSetPooledPolicy<T> : PooledObjectPolicy<HashSet<T>>
{
    public IEqualityComparer<T> Comparer { get; set; } = EqualityComparer<T>.Default;
    public int InitialCapacity { get; set; } = 1024 / 8;
    public int MaximumRetainedCapacity { get; set; } = 1024;

    public override HashSet<T> Create( ) => new( InitialCapacity, Comparer );

    public override bool Return( HashSet<T> value )
    {
        ArgumentNullException.ThrowIfNull( value );

        if( value.Capacity > MaximumRetainedCapacity )
        {
            // Too big. Discard this one.
            return false;
        }

        value.Clear();
        return true;
    }
}

public static class ObjectPoolProviderExtensions
{
    public static ObjectPool<HashSet<T>> CreateHashSetPool<T>( this ObjectPoolProvider provider, HashSetPooledPolicy<T>? policy = default )
    {
        ArgumentNullException.ThrowIfNull( provider );
        return provider.Create( policy ?? new() );
    }
}