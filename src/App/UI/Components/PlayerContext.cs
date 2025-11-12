using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components;

public sealed class PlayerContext : IAsyncDisposable
{
    private CancellationTokenSource cancellation = new();
    private OnMetaChangedEvent? metadata;

    private readonly SemaphoreSlim locker = new( 1, 1 );

    private readonly List<PlayerChangeSubscriber> changing = [];
    private readonly List<PlayerChangeSubscriber> changed = [];

    public MediaMetadata? Metadata => Station?.Id == metadata?.StationId ? metadata?.Metadata : default;
    public Station? Station { get; private set; }

    public async ValueTask DisposeAsync( )
    {
        if( cancellation is not null )
        {
            await cancellation.CancelAsync();
            cancellation.Dispose();

            cancellation = default!;
        }

        locker.Dispose();

        changing.Clear();
        changed.Clear();
    }

    public IDisposable OnChanging( PlayerChangeSubscriber subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );
        return new OnChangingSubscription( this, subscriber );
    }

    public IDisposable OnChanged( PlayerChangeSubscriber subscriber )
    {
        ArgumentNullException.ThrowIfNull( subscriber );
        return new OnChangedSubscription( this, subscriber );
    }

    internal async ValueTask Update( Station? station )
    {
        if( Station is null && station is null && metadata is null )
        {
            return;
        }

        await CancelAndReset();
        await Invoke( async ( ) =>
        {
            if( Station is null && station is null && metadata is null )
            {
                return;
            }

            if( station is null )
            {
                metadata = default;
            }

            foreach( var subscriber in changing )
            {
                await subscriber.Invoke( station, Metadata );
            }

            Station = station;
            foreach( var subscriber in changed )
            {
                await subscriber.Invoke( Station, Metadata );
            }
        } );
    }

    internal ValueTask UpdateMeta( OnMetaChangedEvent e )
    {
        ArgumentNullException.ThrowIfNull( e );

        if( Station is null || metadata == e )
        {
            return default;
        }

        return Invoke( async ( ) =>
        {
            if( Station is null )
            {
                e = default!;
            }

            foreach( var subscriber in changing )
            {
                await subscriber.Invoke( Station, e?.Metadata );
            }

            metadata = e;
            foreach( var subscriber in changed )
            {
                await subscriber.Invoke( Station, e?.Metadata );
            }
        } );
    }

    private async Task CancelAndReset( )
    {
        await cancellation.CancelAsync();
        if( !cancellation.TryReset() )
        {
            cancellation.Dispose();
            cancellation = new();
        }
    }

    private async ValueTask Invoke( Func<ValueTask> action )
    {
        ArgumentNullException.ThrowIfNull( action );

        try
        {
            await locker.WaitAsync( cancellation.Token );
            try
            {
                await action();
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

    private sealed class OnChangingSubscription : IDisposable
    {
        private readonly PlayerContext context;
        private readonly PlayerChangeSubscriber subscriber;

        public OnChangingSubscription( PlayerContext context, PlayerChangeSubscriber subscriber )
        {
            this.context = context;
            this.subscriber = subscriber;

            context.changing.Add( subscriber );
        }

        public void Dispose( ) => context.changing.Remove( subscriber );
    }

    private sealed class OnChangedSubscription : IDisposable
    {
        private readonly PlayerContext context;
        private readonly PlayerChangeSubscriber subscriber;

        public OnChangedSubscription( PlayerContext context, PlayerChangeSubscriber subscriber )
        {
            this.context = context;
            this.subscriber = subscriber;

            context.changed.Add( subscriber );
        }

        public void Dispose( ) => context.changed.Remove( subscriber );
    }
}

public delegate ValueTask PlayerChangeSubscriber( Station? station, MediaMetadata? metadata );