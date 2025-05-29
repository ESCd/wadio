namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class GetTagsParameters
{
    public bool HideBroken { get; init; }
    public uint? Limit { get; init; }
    public uint? Offset { get; init; }
    public TagOrderBy? Order { get; init; }
    public bool Reverse { get; init; }
}

public enum TagOrderBy : byte
{
    Name,
    StationCount,
}