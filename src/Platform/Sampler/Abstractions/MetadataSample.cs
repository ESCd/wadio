using System.ComponentModel.DataAnnotations;

namespace Wadio.Platform.Sampler.Abstractions;

public sealed record MetadataSample(
    [property: Required, Url]
    Uri SourceUrl,

    [property: Required]
    Guid StationId,

    [property: Required]
    MetadataType Type )
{
    public Ulid Id { get; private init; } = Ulid.NewUlid();

    [MinLength( 1 )]
    [Required]
    public IReadOnlyDictionary<string, string> Data { get; init; }
}

public enum MetadataType
{
    Icecast
}