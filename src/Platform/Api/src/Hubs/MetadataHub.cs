using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Hubs;

public sealed class MetadataHub( IMetadataWorkerContext worker ) : Hub
{
    [HubMethodName( nameof( MetadataSignal.Metadata ) )]
    public async IAsyncEnumerable<IReadOnlyDictionary<string, string>> Metadata( Guid stationId, [EnumeratorCancellation] CancellationToken cancellation )
    {
        await using var subscription = await worker.Subscribe( stationId, cancellation );
        await foreach( var metadata in subscription.Read( cancellation ) )
        {
            if( metadata is null )
            {
                yield break;
            }

            yield return metadata;
        }
    }
}