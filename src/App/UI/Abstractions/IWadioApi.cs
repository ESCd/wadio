using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Wadio.App.UI.Abstractions;

public interface IWadioApi
{
    public ICountriesApi Countries { get; }
    public ILanguagesApi Languages { get; }
    public IStationsApi Stations { get; }
    public ITagsApi Tags { get; }
}

public interface ICountriesApi
{
    public IAsyncEnumerable<Country> Get( CancellationToken cancellation = default );
}

public interface ILanguagesApi
{
    public IAsyncEnumerable<Language> Get( CancellationToken cancellation = default );
}

public interface IStationsApi
{
    public ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default );
    public Task<Station?> Random( CancellationToken cancellation = default );
    public IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, CancellationToken cancellation = default );
}

public interface ITagsApi
{
    public IAsyncEnumerable<Tag> Get( CancellationToken cancellation = default );
}

public sealed record Country( string Code, string Name, uint StationCount );
public sealed record Language( string Code, string Name, uint StationCount );
public sealed record Tag( string Name, uint StationCount );

public sealed record class SearchStationsParameters
{
    public Codec? Codec { get; set; }

    [DefaultValue( 25u )]
    public uint Count { get; set; } = 25u;
    public string? CountryCode { get; set; }
    public string? LanguageCode { get; set; }
    public string? Name { get; set; }
    public uint? Offset { get; set; }

    [DefaultValue( StationOrderBy.Name )]
    public StationOrderBy? Order { get; set; } = StationOrderBy.Name;
    public bool? Reverse { get; set; }
    public string? Tag { get; set; }
    public string[] Tags { get; set; } = [];
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
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public StationMetrics Metrics { get; init; } = StationMetrics.Zero;
    public string[] Languages { get; init; } = [];
    public string? State { get; init; }
    public string[] Tags { get; init; } = [];
    public DateTimeOffset? UpdatedAt { get; init; }
}

public enum StationOrderBy : byte
{
    [Display( Name = "Last Viewed" )]
    LastViewed,

    [Display( Name = "Most Viewed" )]
    MostViewed,
    Name,
    Random,

    [Display( Name = "Recently Updated" )]
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

    [Display( Name = "AAC+" )]
    AACPlus,

    [Display( Name = "AAC, H.264" )]
    AACH264,

    [Display( Name = "AAC+, H.264" )]
    AACPlusH264,
    FLAC,
    FLV,
    MP3,
    OGG,
}

public static class CodecString
{
    public static string Format( Codec codec ) => codec switch
    {
        Codec.AACH264 => "aac,h.264",
        Codec.AACPlus => "aac+",
        Codec.AACPlusH264 => "aac+,h.264",
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
            "AAC,H.264" or "aac,h.264" => Codec.AACH264,
            "AAC+" or "aac+" => Codec.AACPlus,
            "AAC+,H.264" or "aac+,h.264" => Codec.AACPlusH264,
            _ => Codec.Unknown,
        };
    }
}