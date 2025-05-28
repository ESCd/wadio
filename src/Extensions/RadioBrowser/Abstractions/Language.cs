using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class Language
{
    [JsonPropertyName( "iso_639" )]
    public string Code { get; init; }
    public string Name { get; init; }

    [JsonPropertyName( "stationcount" )]
    public uint StationCount { get; init; }
}