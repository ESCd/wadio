namespace Wadio.App.UI.Components;

public sealed class SearchContext
{
    private readonly List<SearchRequestSubscriber> subscribers = [];

    public IDisposable OnSearchRequested( SearchRequestSubscriber subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );

        // NOTE: insert at `0`, so that subscribers are invoked in reverse
        subscribers.Insert( 0, subscriber );

        return new Subscription( subscriber, subscribers );
    }

    public async ValueTask<bool> Search( string? query, CancellationToken cancellation = default )
    {
        var e = new SearchRequestEventArgs( query );
        foreach( var subscriber in subscribers )
        {
            await subscriber( e, cancellation );
            if( !e.Continue )
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class SearchRequestEventArgs( string? query ) : EventArgs
{
    public bool Continue { get; private set; }
    public string? Query { get; } = query;

    public void Handled( ) => Continue = false;
}

public delegate ValueTask SearchRequestSubscriber( SearchRequestEventArgs e, CancellationToken cancellation );

sealed file class Subscription( SearchRequestSubscriber subscriber, IList<SearchRequestSubscriber> subscribers ) : IDisposable
{
    public void Dispose( ) => subscribers.Remove( subscriber );
}