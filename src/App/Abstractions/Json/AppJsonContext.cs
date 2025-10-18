using System.Text.Json.Serialization;
using Wadio.App.Abstractions.Api;
using Wadio.App.Abstractions.Signals;

namespace Wadio.App.Abstractions.Json;

[JsonSerializable( typeof( ApiProblem ) )]
[JsonSerializable( typeof( WadioVersion ) )]
[JsonSerializable( typeof( Country ) )]
[JsonSerializable( typeof( Language ) )]
[JsonSerializable( typeof( Release ) )]
[JsonSerializable( typeof( Station ) )]
[JsonSerializable( typeof( Tag ) )]

[JsonSerializable( typeof( MetadataSignals.Metadata ) )]

[JsonSourceGenerationOptions( DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false )]
public sealed partial class AppJsonContext : JsonSerializerContext;