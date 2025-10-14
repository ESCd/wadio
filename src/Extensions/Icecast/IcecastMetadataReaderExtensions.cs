using System.Runtime.CompilerServices;
using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.Extensions.Icecast;

public static class IcecastMetadataReaderExtensions
{
    public static async IAsyncEnumerable<IcecastMetadataDictionary> AsAsyncEnumerable( this IcecastMetadataReader reader, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( reader );

        while( !cancellation.IsCancellationRequested )
        {
            yield return await reader.WaitUntilMetadata( cancellation ).ConfigureAwait( false );
        }
    }

    public static async Task<IcecastMetadataDictionary> WaitUntilMetadata( this IcecastMetadataReader reader, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( reader );

        if( reader.IsFaulted )
        {
            throw reader.Exception ?? new InvalidOperationException( $"The {nameof( IcecastMetadataReader )} has entered a faulted state." );
        }

        var completion = new TaskCompletionSource<IcecastMetadataDictionary>();
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

        void OnEnded( Exception? exception ) => completion.TrySetException( exception ?? new EndOfStreamException( "The icecast stream has ended." ) );

        ValueTask OnMetadata( IcecastMetadataDictionary metadata )
        {
            completion.TrySetResult( metadata );
            return ValueTask.CompletedTask;
        }
    }

    public static async Task WaitUntilEnded( this IcecastMetadataReader reader, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( reader );

        var completion = new TaskCompletionSource();
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

        void OnEnded( Exception? e )
        {
            if( e is not null )
            {
                completion.TrySetException( e );
                return;
            }

            completion.TrySetResult();
        }
    }
}