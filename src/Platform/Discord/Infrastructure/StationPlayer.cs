using System.Collections.Concurrent;

namespace Wadio.Platform.Discord.Infrastructure;

internal abstract class StationPlayer( ConcurrentDictionary<Guid, StationPlayerEntry> players, Guid stationId ) : IAsyncDisposable
{
    public Guid StationId => stationId;

    public abstract event Action<Exception?> Ended;
    public abstract event Func<IReadOnlyDictionary<string, string>, ValueTask> MetadataUpdated;

    public abstract ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default );

    public async ValueTask DisposeAsync( )
    {
        if( players.TryGetValue( stationId, out var entry ) )
        {
            var updated = entry with
            {
                Count = Math.Max( 0, entry.Count - 1 )
            };

            if( players.TryUpdate( stationId, updated, entry ) )
            {
                if( updated.Count is 0 && players.Remove( stationId, out var removed ) )
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