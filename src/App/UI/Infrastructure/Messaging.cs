namespace Wadio.App.UI.Infrastructure;

public interface IMessenger
{
    public ValueTask Dispatch<T>( T message, CancellationToken cancellation = default )
        where T : Message;

    public IDisposable Subscribe<T>( MessageSubscriber<T> subscriber )
        where T : Message;
}

public abstract record Message;
public delegate ValueTask MessageSubscriber<T>( T message, CancellationToken cancellation )
    where T : Message;

internal sealed class Messenger : IMessenger
{
    private event Func<object, CancellationToken, ValueTask> OnDispatch;

    public ValueTask Dispatch<T>( T message, CancellationToken cancellation = default )
        where T : Message
    {
        ArgumentNullException.ThrowIfNull( message );
        return OnDispatch?.Invoke( message, cancellation ) ?? default;
    }

    public IDisposable Subscribe<T>( MessageSubscriber<T> subscriber )
        where T : Message
    {
        ArgumentNullException.ThrowIfNull( subscriber );
        return new MessageSubscription<T>( this, subscriber );
    }

    private sealed class MessageSubscription<T> : IDisposable
        where T : Message
    {
        private readonly Messenger messenger;
        private readonly MessageSubscriber<T> subscriber;

        public MessageSubscription( Messenger messenger, MessageSubscriber<T> subscriber )
        {
            this.messenger = messenger;
            this.subscriber = subscriber;

            messenger.OnDispatch += OnDispatch;
        }

        public void Dispose( )
        {
            messenger.OnDispatch -= OnDispatch;
        }

        private ValueTask OnDispatch( object value, CancellationToken cancellation = default )
        {
            ArgumentNullException.ThrowIfNull( value );
            if( value is not T typed )
            {
                return default;
            }

            return subscriber( typed, cancellation );
        }
    }
}
