using System.Runtime.CompilerServices;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.Extensions.Icecast;

public static class IcecastMetadataReaderExtensions
{
    public static async IAsyncEnumerable<IcecastMetadataDictionary> AsAsyncEnumerable( this IcecastStreamReader reader, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( reader );

        while( !cancellation.IsCancellationRequested )
        {
            yield return await reader.WaitUntilMetadata( cancellation ).ConfigureAwait( false );
        }
    }

    public static void ThrowIfFaulted( this IcecastStreamReader reader )
    {
        ArgumentNullException.ThrowIfNull( reader );

        if( reader.IsFaulted )
        {
            throw reader.Exception ?? new InvalidOperationException( $"The {nameof( IcecastStreamReader )} has entered a faulted state." );
        }
    }

    public static async ValueTask<IcecastMetadataDictionary> WaitUntilMetadata( this IcecastStreamReader reader, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( reader );
        ThrowIfFaulted( reader );

        var completion = new TaskCompletionSource<IcecastMetadataDictionary>( TaskCreationOptions.RunContinuationsAsynchronously );
        try
        {
            await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
            {
                reader.Ended += OnEnded;
                reader.MetadataRead += OnMetadata;
                return await completion.Task.ConfigureAwait( false );
            }
        }
        finally
        {
            reader.MetadataRead -= OnMetadata;
            reader.Ended -= OnEnded;
        }

        void OnCancelled( ) => completion.TrySetCanceled( cancellation );
        ValueTask OnEnded( Exception? exception )
        {
            completion.TrySetException( exception ?? new EndOfStreamException( "The icecast stream has ended." ) );
            return default;
        }

        ValueTask OnMetadata( IcecastMetadataDictionary metadata )
        {
            completion.SetResult( metadata );
            return ValueTask.CompletedTask;
        }
    }

    public static async ValueTask WaitUntilEnded( this IcecastStreamReader reader, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( reader );

        if( reader.IsFaulted )
        {
            return;
        }

        var completion = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
        try
        {
            await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
            {
                reader.Ended += OnEnded;
                await completion.Task.ConfigureAwait( false );
            }
        }
        finally
        {
            reader.Ended -= OnEnded;
        }

        void OnCancelled( ) => completion.TrySetCanceled( cancellation );

        ValueTask OnEnded( Exception? e )
        {
            if( e is not null )
            {
                completion.TrySetException( e );
                return default;
            }

            completion.SetResult();
            return default;
        }
    }
}