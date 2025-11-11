using System.Collections.Immutable;

namespace Wadio.App.UI.Infrastructure;

public static class AsyncEnumerableExtensions
{
    public static async ValueTask<ImmutableArray<T>> ToImmutableArrayAsync<T>( this IAsyncEnumerable<T> source, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( source );

        return [ .. await source.ToListAsync( cancellation ) ];
    }

    public static async ValueTask<ImmutableDictionary<TKey, TValue>> ToImmutableDictionaryAsync<TKey, TValue>( this IAsyncEnumerable<TValue> source, Func<TValue, TKey> selector, CancellationToken cancellation = default )
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull( source );

        var value = await source.ToDictionaryAsync( selector, cancellationToken: cancellation );
        return value.ToImmutableDictionary();
    }
}