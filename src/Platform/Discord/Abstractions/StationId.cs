namespace Wadio.Platform.Discord.Abstractions;

public readonly struct StationId( Guid value )
{
    private readonly Guid value = value;

    public static implicit operator Guid( StationId stationId ) => stationId.value;
}
