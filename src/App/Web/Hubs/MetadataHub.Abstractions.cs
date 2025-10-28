using Wadio.Extensions.Icecast.Abstractions;

namespace Wadio.App.Web.Hubs;

public interface IMetadataWorkerContext
{
    internal ValueTask<MetadataWorkerRequest> Next( CancellationToken cancellation );

    public Task<IMetadataWorkerSubscription> Subscribe( Guid stationId, CancellationToken cancellation );
}

public interface IMetadataWorkerSubscription : IAsyncDisposable
{
    public IAsyncEnumerable<IcecastMetadataDictionary?> Read( CancellationToken cancellation );
}

internal sealed record MetadataWorkerRequest( Guid StationId )
{
    public TaskCompletionSource<IMetadataWorkerSubscription> Completion { get; } = new();
}