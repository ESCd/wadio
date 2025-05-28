using System.Text.Json.Serialization;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.UI.Json;

[JsonSerializable( typeof( ApiProblem ) )]
[JsonSerializable( typeof( AppVersion ) )]
[JsonSerializable( typeof( Country ) )]
[JsonSerializable( typeof( Language ) )]
[JsonSerializable( typeof( Station ) )]

[JsonSourceGenerationOptions( DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false )]
public sealed partial class AppJsonContext : JsonSerializerContext;