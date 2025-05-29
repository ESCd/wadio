using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class StationVote
{
    public string Message { get; init; }

    [JsonPropertyName( "ok" )]
    public bool Success { get; init; }
}