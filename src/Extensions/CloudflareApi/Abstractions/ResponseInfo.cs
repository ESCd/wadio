using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Wadio.Extensions.CloudflareApi.Abstractions;

public sealed record ResponseInfo(
    [property: Range(1000, int.MaxValue)]
    int Code,
    [property: JsonPropertyName("documentation_url")]
    Uri? DocumentationUrl,
    string Message );