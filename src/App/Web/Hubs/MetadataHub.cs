using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

namespace Wadio.App.Web.Hubs;

public sealed class MetadataHub( IMetadataWorkerContext worker ) : Hub
{
    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> Metadata( Guid stationId, [EnumeratorCancellation] CancellationToken cancellation )
    {
        await using var subscription = await worker.Subscribe( stationId, cancellation );
        await foreach( var metadata in subscription.Read( cancellation ) )
        {
            yield return metadata;
        }
    }
}