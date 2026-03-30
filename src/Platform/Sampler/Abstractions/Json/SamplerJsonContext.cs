using System.Text.Json.Serialization;

namespace Wadio.Platform.Sampler.Abstractions.Json;

[JsonSerializable( typeof( MetadataSample ) )]

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false )]
public sealed partial class SamplerJsonContext : JsonSerializerContext;