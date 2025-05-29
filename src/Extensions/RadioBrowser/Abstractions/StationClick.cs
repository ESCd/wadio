using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class StationClick
{
    public string Message { get; init; }
    public string Name { get; init; }

    [JsonPropertyName( "stationuuid" )]
    public Guid StationId { get; init; }

    [JsonPropertyName( "ok" )]
    public bool Success { get; init; }

    public Uri Url { get; init; }
}