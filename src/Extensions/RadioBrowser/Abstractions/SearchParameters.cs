namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class SearchParameters
{
    public string? Codec { get; init; }
    public string? CountryCode { get; init; }
    public bool? HideBroken { get; init; } = true;
    public bool? IsHttps { get; init; }
    public string? Language { get; init; }
    public uint? Limit { get; init; }
    public string? Name { get; init; }
    public uint? Offset { get; init; }
    public StationOrderBy? Order { get; init; }
    public bool? Reverse { get; init; }
    public string? State { get; init; }
    public string[]? Tags { get; init; }
}

public enum StationOrderBy
{
    Name,
    Url,
    Homepage,
    Favicon,
    Tags,
    Country,
    State,
    Language,
    Votes,
    Codec,
    Bitrate,
    LastCheckOk,
    LastCheckTime,
    ClickTimestamp,
    ClickCount,
    ClickTrend,
    ChangeTimestamp,
    Random
}