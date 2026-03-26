namespace Wadio.Platform.Discord.Infrastructure;

internal static class Disposer
{
    public static async ValueTask DisposeAsync( ICollection<IAsyncDisposable> disposables )
    {
        ArgumentNullException.ThrowIfNull( disposables );

        foreach( var disposable in disposables )
        {
            await disposable.DisposeAsync();
        }

        disposables.Clear();
    }

    public static async ValueTask DisposeAsync<TKey, TValue>( IDictionary<TKey, TValue> disposables )
        where TValue : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull( disposables );

        foreach( var disposable in disposables.Values )
        {
            await disposable.DisposeAsync();
        }

        disposables.Clear();
    }

    public static async ValueTask DisposeAsync( params IEnumerable<IAsyncDisposable> disposables )
    {
        ArgumentNullException.ThrowIfNull( disposables );

        foreach( var disposable in disposables )
        {
            await disposable.DisposeAsync();
        }
    }
}