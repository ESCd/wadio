using System.Text.Json.Serialization;
using Wadio.Platform.Abstractions;

namespace Wadio.Platform.Api.Abstractions.Json;

[JsonSerializable( typeof( ApiProblem ) )]
[JsonSerializable( typeof( WadioVersion ) )]

[JsonSerializable( typeof( Country ) )]
[JsonSerializable( typeof( Language ) )]
[JsonSerializable( typeof( Release ) )]
[JsonSerializable( typeof( Station ) )]
[JsonSerializable( typeof( Tag ) )]

[JsonSerializable( typeof( MetadataSignal.Metadata ) )]

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false )]
public sealed partial class ApiJsonContext : JsonSerializerContext;