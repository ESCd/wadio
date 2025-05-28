namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record class GetCountriesParameters
{
    public bool HideBroken { get; init; }
    public uint? Limit { get; init; }
    public uint? Offset { get; init; }
    public CountryOrderBy? Order { get; init; }
    public bool Reverse { get; init; }
}

public enum CountryOrderBy : byte
{
    Name,
    StationCount,
}