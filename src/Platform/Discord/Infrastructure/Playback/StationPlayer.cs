using System.Collections.Concurrent;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal abstract class StationPlayer( ConcurrentDictionary<Guid, StationPlayerEntry> players, Station station ) : IAsyncDisposable
{
    public Station Station => station;
    public Codec Codec => station.Codec;

    public abstract event Func<Exception?, ValueTask> Ended;
    public abstract event Func<IReadOnlyDictionary<string, string>, ValueTask> MetadataUpdated;

    public abstract ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default );

    public async ValueTask DisposeAsync( )
    {
        if( players.TryGetValue( station.Id, out var entry ) )
        {
            var updated = entry with
            {
                Count = Math.Max( 0, entry.Count - 1 )
            };

            if( players.TryUpdate( station.Id, updated, entry ) )
            {
                if( updated.Count is 0 && players.Remove( station.Id, out var removed ) )
                {
                    await removed.Value.OnDisposeAsync();
                    if( removed.Value != this )
                    {
                        await OnDisposeAsync();
                    }
                }
            }
        }
    }

    protected virtual ValueTask OnDisposeAsync( ) => default;
}

internal static class StationPlayerExtensions
{
    public static async Task<IReadOnlyDictionary<string, string>> WaitUntilMetadata( this StationPlayer player, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( player );

        var completion = new TaskCompletionSource<IReadOnlyDictionary<string, string>>( TaskCreationOptions.RunContinuationsAsynchronously );
        try
        {
            await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
            {
                player.Ended += OnEnded;
                player.MetadataUpdated += OnMetadata;
                return await completion.Task.ConfigureAwait( false );
            }
        }
        finally
        {
            player.MetadataUpdated -= OnMetadata;
            player.Ended -= OnEnded;
        }

        void OnCancelled( ) => completion.TrySetCanceled( cancellation );
        ValueTask OnEnded( Exception? exception )
        {
            completion.TrySetException( exception ?? new EndOfStreamException( "The player has ended." ) );
            return default;
        }

        ValueTask OnMetadata( IReadOnlyDictionary<string, string> metadata )
        {
            completion.TrySetResult( metadata );
            return ValueTask.CompletedTask;
        }
    }
}