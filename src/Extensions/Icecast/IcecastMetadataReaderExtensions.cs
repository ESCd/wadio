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

        var completion = new TaskCompletionSource<IcecastMetadataDictionary>();
        await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
        {
            reader.Ended += OnEnded;
            reader.MetadataRead += OnMetadata;
            return await completion.Task.ConfigureAwait( false );
        }

        void OnCancelled( )
        {
            reader.Ended -= OnEnded;
            reader.MetadataRead -= OnMetadata;
            completion.SetCanceled( cancellation );
        }

        void OnEnded( Exception? exception )
        {
            reader.Ended -= OnEnded;
            reader.MetadataRead -= OnMetadata;
            completion.SetException( exception ?? new InvalidOperationException() );
        }

        ValueTask OnMetadata( IcecastMetadataDictionary metadata )
        {
            reader.Ended -= OnEnded;
            reader.MetadataRead -= OnMetadata;

            completion.SetResult( metadata );
            return ValueTask.CompletedTask;
        }
    }

    public static async Task WaitUntilEnded( this IcecastMetadataReader reader, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( reader );

        var completion = new TaskCompletionSource();
        await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
        {
            reader.Ended += OnEnded;
            await completion.Task.ConfigureAwait( false );
        }

        void OnCancelled( )
        {
            reader.Ended -= OnEnded;
            completion.SetCanceled( cancellation );
        }

        void OnEnded( Exception? e )
        {
            if( e is not null )
            {
                completion.SetException( e );
                return;
            }

            completion.SetResult();
        }
    }
}