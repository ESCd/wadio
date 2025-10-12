using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Components;

public sealed class PlayerContext : IAsyncDisposable
{
    private CancellationTokenSource cancellation = new();

    private readonly SemaphoreSlim locker = new( 1, 1 );
    private readonly List<PlayerChangeSubscriber> onChanging = [];
    private readonly List<PlayerChangeSubscriber> onChanged = [];

    public Station? Station { get; private set; }

    public async ValueTask DisposeAsync( )
    {
        await cancellation.CancelAsync();
        cancellation.Dispose();

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

    public async ValueTask Update( Station? station )
    {
        if( Station?.Id == station?.Id )
        {
            return;
        }

        await cancellation.CancelAsync();
        if( !cancellation.TryReset() )
        {
            cancellation.Dispose();
            cancellation = new();
        }

        try
        {
            await locker.WaitAsync( cancellation.Token );
            try
            {
                foreach( var subscriber in onChanging )
                {
                    await subscriber( station );
                }

                Station = station;

                foreach( var subscriber in onChanged )
                {
                    await subscriber( station );
                }
            }
            finally
            {
                locker.Release();
            }
        }
        catch( TaskCanceledException e ) when( e.CancellationToken == cancellation.Token )
        {
            // NOTE: ignore
        }
    }
}

public delegate ValueTask PlayerChangeSubscriber( Station? station );

sealed file class Subscription( PlayerChangeSubscriber subscriber, List<PlayerChangeSubscriber> subscribers ) : IDisposable
{
    public void Dispose( ) => subscribers.Remove( subscriber );
}