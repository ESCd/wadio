using Wadio.App.UI.Abstractions;

namespace Wadio.App.UI.Components;

public sealed class PlayerContext : IDisposable
{
    private readonly SemaphoreSlim locker = new( 1, 1 );
    private readonly List<PlayerChangeSubscriber> onChanging = [];
    private readonly List<PlayerChangeSubscriber> onChanged = [];

    public Station? Station { get; private set; }

    public void Dispose( )
    {
        locker.Dispose();
        onChanging.Clear();
        onChanged.Clear();
    }

    public IDisposable OnChanging( PlayerChangeSubscriber subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );

        onChanging.Add( subscriber );
        return new Subscription( subscriber, onChanging );
    }

    public IDisposable OnChanged( PlayerChangeSubscriber subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );

        onChanged.Add( subscriber );
        return new Subscription( subscriber, onChanged );
    }

    public async ValueTask Update( Station? station, CancellationToken cancellation = default )
    {
        if( Station?.Id == station?.Id )
        {
            return;
        }

        await locker.WaitAsync( cancellation );
        try
        {
            foreach( var subscriber in onChanging )
            {
                await subscriber( station, cancellation );
            }

            Station = station;

            foreach( var subscriber in onChanged )
            {
                await subscriber( station, cancellation );
            }
        }
        finally
        {
            locker.Release();
        }
    }
}

public delegate ValueTask PlayerChangeSubscriber( Station? station, CancellationToken cancellation );

sealed file class Subscription( PlayerChangeSubscriber subscriber, List<PlayerChangeSubscriber> subscribers ) : IDisposable
{
    public void Dispose( ) => subscribers.Remove( subscriber );
}