using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class ServiceStatistics
{
    [JsonPropertyName( "stations_broken" )]
    public uint BrokenStations { get; init; }
    public uint ClicksLastHour { get; init; }
    public uint ClicksLastDay { get; init; }
    public uint Countries { get; init; }
    public uint Languages { get; init; }
    public string SoftwareVersion { get; init; }
    public uint Stations { get; init; }
    public string Status { get; init; }
    public int SupportedVersion { get; init; }
    public uint Tags { get; init; }
}