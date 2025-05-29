using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class Tag
{
    public string Name { get; init; }

    [JsonPropertyName( "stationcount" )]
    public uint StationCount { get; init; }
}