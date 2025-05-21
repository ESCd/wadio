using System.ComponentModel;
using System.Runtime.Serialization;

namespace Wadio.App.UI.Abstractions;

public interface IWadioApi
{
    public IStationsApi Stations { get; }
}

public interface IStationsApi
{
    public ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default );
    public Task<Station?> Random( CancellationToken cancellation = default );
    public IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, CancellationToken cancellation = default );
}

public sealed record class SearchStationsParameters
{
    public Codec? Codec { get; init; }

    [DefaultValue( 25u )]
    public uint Count { get; init; } = 25u;
    public uint? Offset { get; init; }

    [DefaultValue( StationOrderBy.Name )]
    public StationOrderBy Order { get; init; } = StationOrderBy.Name;
    public bool? Reverse { get; init; }
}

public sealed record Station( Guid Id, string Name, Uri Url )
{
    public DateTimeOffset? CheckedAt { get; init; }
    public uint? Bitrate { get; init; }
    public Codec Codec { get; init; }
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public Uri? HomepageUrl { get; init; }
    public Uri? IconUrl { get; init; }
    public bool IsHls { get; init; }
    public StationMetrics Metrics { get; init; } = StationMetrics.Zero;
    public string[] Languages { get; init; } = [];
    public string? State { get; init; }
    public string[] Tags { get; init; } = [];
    public DateTimeOffset? UpdatedAt { get; init; }
}

public enum StationOrderBy : byte
{
    LastViewed,
    MostViewed,
    Name,
    Random,
    RecentlyUpdated,
    Trending,
    Votes,
}

public sealed record StationMetrics( ulong Plays, int Trend, ulong Votes )
{
    public static readonly StationMetrics Zero = new( 0, 0, 0 );
}

public enum Codec : byte
{
    Unknown,
    AAC,
    AACPlus,
    MP3
}

public static class CodecString
{
    public static string Format( Codec codec ) => codec switch
    {
        Codec.AACPlus => "aac+",
        _ => codec.ToString().ToLowerInvariant(),
    };

    public static Codec Parse( string? value )
    {
        if( Enum.TryParse<Codec>( value, out var codec ) )
        {
            return codec;
        }

        return value switch
        {
            "AAC+" or "aac+" => Codec.AACPlus,
            _ => Codec.Unknown,
        };
    }
}