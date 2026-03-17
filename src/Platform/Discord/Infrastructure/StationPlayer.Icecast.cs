using System.Collections.Concurrent;
using Wadio.Extensions.Icecast;

namespace Wadio.Platform.Discord.Infrastructure;

internal sealed class IcecastStationPlayer : StationPlayer
{
    private readonly IcecastStreamReader icecast;

    public override event Action<Exception?> Ended;
    public override event Func<IReadOnlyDictionary<string, string>, ValueTask> MetadataUpdated;

    public IcecastStationPlayer(
        IcecastStreamReader icecast,
        ConcurrentDictionary<Guid, StationPlayerEntry> players,
        Guid stationId ) : base( players, stationId )
    {
        this.icecast = icecast;

        icecast.Ended += e => Ended?.Invoke( e );
        icecast.MetadataRead += metadata => MetadataUpdated?.Invoke( metadata ) ?? default;
    }

    public override ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default ) => new( icecast.CreateAudioStream() );

    protected override ValueTask OnDisposeAsync( ) => icecast.DisposeAsync();
}