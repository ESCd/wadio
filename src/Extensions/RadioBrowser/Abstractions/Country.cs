using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class Country
{
    [JsonPropertyName( "iso_3166_1" )]
    public string Code { get; init; }
    public string Name { get; init; }

    [JsonPropertyName( "stationcount" )]
    public uint StationCount { get; init; }
}