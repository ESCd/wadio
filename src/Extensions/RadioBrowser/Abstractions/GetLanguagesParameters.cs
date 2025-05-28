namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class GetLanguagesParameters
{
    public bool HideBroken { get; init; }
    public uint? Limit { get; init; }
    public uint? Offset { get; init; }
    public LanguageOrderBy? Order { get; init; }
    public bool Reverse { get; init; }
}

public enum LanguageOrderBy : byte
{
    Name,
    StationCount,
}