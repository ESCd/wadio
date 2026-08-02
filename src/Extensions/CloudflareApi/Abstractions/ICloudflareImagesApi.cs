using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Numerics;

namespace Wadio.Extensions.CloudflareApi.Abstractions;

public interface ICloudflareImagesApi
{
    public Task<DeleteImageResponse> DeleteAsync( string imageId, CancellationToken cancellation = default );
    public Task<ListImagesResponse> ListAsync( ListImagesRequest request, CancellationToken cancellation = default );
    public Task<StatsResponse> StatsAsync( CancellationToken cancellation = default );
    public Task<UploadImageResponse> UploadAsync( UploadImageRequest request, CancellationToken cancellationToken = default );
}

public sealed record DeleteImageResponse(
    IReadOnlyCollection<ResponseInfo> Errors,
    IReadOnlyCollection<ResponseInfo> Messages );

public sealed record Image(
    [property: StringLength(32)]
    string? Id,
    [property: StringLength( 255 )]
    string? Filename )
{
    [StringLength( 1024 )]
    public string? Creator { get; init; }

    public IReadOnlyDictionary<string, string>? Meta { get; init; }

    public DateTimeOffset? Uploaded { get; init; }

    public IReadOnlyCollection<string>? Variants { get; init; }
}

public sealed record ListImagesRequest
{
    [StringLength( 32 )]
    public string? ContinuationToken { get; init; }

    [StringLength( 1024 )]
    public string? Creator { get; init; }

    [MaxLength( 5 )]
    public IList<ImageMetaFilter>? Meta { get; init; }

    [Range( 10, 10000 )]
    public int? PerPage { get; init; }

    public SortOrder? Sort { get; init; }
}

public sealed record ListImagesResponse(
    IReadOnlyCollection<ResponseInfo> Errors,
    IReadOnlyCollection<ResponseInfo> Messages,
    ListImagesResult? Result );

public sealed record ListImagesResult( IReadOnlyCollection<Image>? Images )
{
    public string? ContinuationToken { get; init; }
}

public abstract record ImageMetaFilter
{
    public string Key { get; private set; }
    public string Value { get; private set; }

    private ImageMetaFilter( string key, string value )
    {
        Key = key;
        Value = value;
    }

    public static ImageMetaFilter Equals( string key, string value )
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new EqualsStringFilter( key, value );
    }

    public static ImageMetaFilter Equals<T>( string key, T value )
        where T : notnull, INumber<T>
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new EqualsNumberFilter( key, value.ToString( default, CultureInfo.InvariantCulture ) );
    }

    public static ImageMetaFilter Equals( string key, bool value )
    {
        ArgumentNullException.ThrowIfNull( key );

        return new EqualsBooleanFilter( key, value.ToString( CultureInfo.InvariantCulture ).ToLowerInvariant() );
    }

    public static ImageMetaFilter GreaterThan<T>( string key, T value )
        where T : notnull, INumber<T>
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new GreaterThanFilter( key, value.ToString( default, CultureInfo.InvariantCulture ) );
    }

    public static ImageMetaFilter GreaterThanOrEquals<T>( string key, T value )
        where T : notnull, INumber<T>
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new GreaterThanOrEqualsFilter( key, value.ToString( default, CultureInfo.InvariantCulture ) );
    }

    public static ImageMetaFilter LessThan<T>( string key, T value )
        where T : notnull, INumber<T>
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new LessThanFilter( key, value.ToString( default, CultureInfo.InvariantCulture ) );
    }

    public static ImageMetaFilter LessThanOrEquals<T>( string key, T value )
        where T : notnull, INumber<T>
    {
        ArgumentNullException.ThrowIfNull( key );
        ArgumentNullException.ThrowIfNull( value );

        return new LessThanOrEqualsFilter( key, value.ToString( default, CultureInfo.InvariantCulture ) );
    }

    public string GetOperatorString( ) => this switch
    {
        EqualsStringFilter => "[eq:string]",
        EqualsNumberFilter => "[eq:number]",
        EqualsBooleanFilter => "[eq:boolean]",
        GreaterThanFilter => "[gt:number]",
        GreaterThanOrEqualsFilter => "[gte:number]",
        LessThanFilter => "[lt:number]",
        LessThanOrEqualsFilter => "[lte:number]",

        _ => throw new NotSupportedException( $"The filter type '{GetType().Name}' is not supported." )
    };

    public override string ToString( ) => $"meta.{Key}{GetOperatorString()}={Value}";

    private sealed record EqualsStringFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record EqualsNumberFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record EqualsBooleanFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record GreaterThanFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record GreaterThanOrEqualsFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record LessThanFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
    private sealed record LessThanOrEqualsFilter( string Key, string Value ) : ImageMetaFilter( Key, Value );
}

public sealed record Stat( StatCount? Count );
public sealed record StatCount( uint Allowed, uint Current );

public sealed record StatsResponse(
    IReadOnlyCollection<ResponseInfo> Errors,
    IReadOnlyCollection<ResponseInfo> Messages,
    Stat? Result );

public abstract record UploadImageRequest
{
    [StringLength( 1024 )]
    public string? Creator { get; init; }

    [StringLength( 32 )]
    public string? Id { get; init; }

    public IDictionary<string, string>? Meta { get; init; }

    public bool? RequireSignedUrls { get; init; }

    public sealed record File( Stream Data, string? FileName, string? ContentType = default ) : UploadImageRequest;
    public sealed record Url( Uri Value ) : UploadImageRequest;
}

public sealed record UploadImageResponse(
    IReadOnlyCollection<ResponseInfo> Errors,
    IReadOnlyCollection<ResponseInfo> Messages,
    Image? Result );