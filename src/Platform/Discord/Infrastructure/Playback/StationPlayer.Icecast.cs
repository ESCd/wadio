using System.Collections.Concurrent;
using Wadio.Extensions.Icecast;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class IcecastStationPlayer : StationPlayer
{
    private readonly IcecastStreamReader reader;

    public override event Func<Exception?, ValueTask> Ended;
    public override event Func<IReadOnlyDictionary<string, string>, ValueTask> MetadataUpdated;

    public IcecastStationPlayer(
        IcecastStreamReader reader,
        ConcurrentDictionary<Guid, StationPlayerEntry> players,
        Station station ) : base( players, station )
    {
        this.reader = reader;

        reader.Ended += e => Ended?.Invoke( e ) ?? default;
        reader.MetadataRead += metadata => MetadataUpdated?.Invoke( metadata ) ?? default;
    }

    public override ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default ) => new( reader.CreateAudioStream() );

    protected override ValueTask OnDisposeAsync( ) => reader.DisposeAsync();
}