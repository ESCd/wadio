using System.Text.Json.Serialization;
using Wadio.Extensions.CloudflareApi.Abstractions;

namespace Wadio.Extensions.CloudflareApi;

[JsonSerializable( typeof( DeleteImageResponse ) )]
[JsonSerializable( typeof( ListImagesResponse ) )]
[JsonSerializable( typeof( StatsResponse ) )]
[JsonSerializable( typeof( UploadImageResponse ) )]

[JsonSourceGenerationOptions(
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull )]
internal sealed partial class CloudflareApiJsonContext : JsonSerializerContext;