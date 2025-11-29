using System.Text.Json.Serialization;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class Station
{
    public uint? Bitrate { get; init; }

    [JsonPropertyName( "changeuuid" )]
    public Guid ChangeId { get; init; }

    [JsonPropertyName( "clickcount" )]
    public ulong ClickCount { get; init; }

    [JsonPropertyName( "clicktrend" )]
    public int ClickTrend { get; init; }
    public string? Codec { get; init; }
    public string Country { get; init; } = string.Empty;

    [JsonPropertyName( "countrycode" )]
    public string? CountryCode { get; init; }

    [JsonConverter( typeof( UrlConverter ) )]
    [JsonPropertyName( "favicon" )]
    public Uri? IconUrl { get; init; }

    [JsonPropertyName( "stationuuid" )]
    public Guid Id { get; init; }

    [JsonConverter( typeof( Json.BitConverter ) )]
    [JsonPropertyName( "hls" )]
    public bool IsHls { get; init; }
    public bool HasExtendedInfo { get; init; }

    [JsonConverter( typeof( UrlConverter ) )]
    [JsonPropertyName( "homepage" )]
    public Uri? HomepageUrl { get; init; }

    [JsonConverter( typeof( CommaDelimitedConverter ) )]
    [JsonPropertyName( "language" )]
    public string[] Languages { get; init; } = [];

    [JsonConverter( typeof( CommaDelimitedConverter ) )]
    [JsonPropertyName( "languagecodes" )]
    public string[] LanguageCodes { get; init; } = [];

    [JsonPropertyName( "lastchangetime_iso8601" )]
    public DateTimeOffset LastChangeTime { get; init; }

    [JsonConverter( typeof( Json.BitConverter ) )]
    public bool LastCheckOk { get; init; }

    [JsonPropertyName( "lastchecktime_iso8601" )]
    public DateTimeOffset LastCheckTime { get; init; }

    [JsonPropertyName( "lastcheckoktime_iso8601" )]
    public DateTimeOffset? LastCheckOkTime { get; init; }

    [JsonPropertyName( "geo_lat" )]
    public double? Latitude { get; init; }

    [JsonPropertyName( "geo_long" )]
    public double? Longitude { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? State { get; init; }

    [JsonConverter( typeof( CommaDelimitedConverter ) )]
    public string[] Tags { get; init; } = [];

    [JsonConverter( typeof( UrlConverter ) )]
    public Uri Url { get; init; } = default!;

    [JsonConverter( typeof( UrlConverter ) )]
    [JsonPropertyName( "url_resolved" )]
    public Uri? ResolvedUrl { get; init; }
    public ulong Votes { get; init; }
}