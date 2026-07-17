using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Wadio.Platform.Api.Abstractions;

public interface IStationsApi
{
    public ValueTask<Station?> Get( Guid stationId, CancellationToken cancellation = default );
    public Task<Station?> Random( SearchStationsParameters? parameters = default, CancellationToken cancellation = default );
    public IAsyncEnumerable<Station> Search( SearchStationsParameters parameters, CancellationToken cancellation = default );
    public Task<bool> Track( Guid stationId, CancellationToken cancellation = default );
    public Task<bool> Vote( Guid stationId, CancellationToken cancellation = default );
}

public sealed record ProximitySearchParameter( double Latitude, double Longitude, double? Radius = default ) : IParsable<ProximitySearchParameter>
{
    public static ProximitySearchParameter Parse( string value, IFormatProvider? provider )
    {
        if( TryParse( value, provider, out var result ) )
        {
            return result;
        }

        throw new FormatException( $"The value '{value}' is not a valid {nameof( ProximitySearchParameter )}." );
    }

    public override string ToString( )
    {
        var value = $"{Latitude},{Longitude}";
        if( Radius.HasValue )
        {
            value += $",{Radius.Value}";
        }

        return value;
    }

    public static bool TryParse( [NotNullWhen( true )] string? value, IFormatProvider? provider, [MaybeNullWhen( false )] out ProximitySearchParameter result )
    {
        if( string.IsNullOrWhiteSpace( value ) )
        {
            result = default;
            return false;
        }

        var parts = value.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
        if( parts.Length is not (2 or 3) )
        {
            result = default;
            return false;
        }

        if( !double.TryParse( parts[ 0 ], NumberStyles.Float, provider, out var latitude ) ||
            !double.TryParse( parts[ 1 ], NumberStyles.Float, provider, out var longitude ) )
        {
            result = default;
            return false;
        }

        result = new( latitude, longitude );
        if( parts.Length is 3 && double.TryParse( parts[ 2 ], NumberStyles.Float, provider, out var radius ) )
        {
            result = result with
            {
                Radius = radius
            };
        }

        return true;
    }

    public static implicit operator ProximitySearchParameter( (double Latitude, double Longitude) coordinate ) => new( coordinate.Latitude, coordinate.Longitude );
    public static implicit operator ProximitySearchParameter( (double Latitude, double Longitude, double? Radius) value ) => new( value.Latitude, value.Longitude, value.Radius );
    public static implicit operator (double Latitude, double Longitude)( ProximitySearchParameter proximity ) => (proximity.Latitude, proximity.Longitude);
    public static implicit operator (double Latitude, double Longitude, double? Radius)( ProximitySearchParameter proximity ) => (proximity.Latitude, proximity.Longitude, proximity.Radius);
}

public sealed record class SearchStationsParameters
{
    public Codec? Codec { get; set; }

    [DefaultValue( 25u )]
    public uint? Count { get; set; } = 25u;
    public string? CountryCode { get; set; }
    public bool? HasLocation { get; set; }
    public string? LanguageCode { get; set; }
    public ProximitySearchParameter? Proximity { get; set; }
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

public sealed record StationMetrics( ulong Plays, int Trend, ulong Votes )
{
    public static readonly StationMetrics Zero = new( 0, 0, 0 );
}

public enum StationOrderBy : byte
{
    [Display( Name = "Last Played" )]
    LastPlayed,

    [Display( Name = "Most Played" )]
    MostPlayed,
    Name,
    Random,

    [Display( Name = "Recently Updated" )]
    RecentlyUpdated,
    Trending,
    Votes,
}