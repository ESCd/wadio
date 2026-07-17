using Wadio.Platform.Sampler.Abstractions;

namespace Wadio.Platform.Sampler.Client.Abstractions;

public interface IMetadataSampler
{
    public ValueTask Sample( MetadataSample sample, CancellationToken cancellation = default );
}