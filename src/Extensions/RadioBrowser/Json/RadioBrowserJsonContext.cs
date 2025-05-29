using System.Text.Json.Serialization;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Json;

[JsonSerializable( typeof( Country ) )]
[JsonSerializable( typeof( Language ) )]
[JsonSerializable( typeof( RadioBrowserHost[] ) )]
[JsonSerializable( typeof( ServiceStatistics ) )]
[JsonSerializable( typeof( Station ) )]
[JsonSerializable( typeof( Tag ) )]

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true,
    WriteIndented = false )]
public sealed partial class RadioBrowserJsonContext : JsonSerializerContext;