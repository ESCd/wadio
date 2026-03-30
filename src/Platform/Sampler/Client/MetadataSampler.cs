using System.Threading.Channels;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Client.Abstractions;

namespace Wadio.Platform.Sampler.Client;

internal sealed class MetadataSampler( Channel<MetadataSample> queue ) : IMetadataSampler
{
    public ValueTask Sample( MetadataSample sample, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( sample );

        return queue.Writer.WriteAsync( sample, cancellation );
    }
}