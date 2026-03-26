using ESCd.Extensions.Http;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Client;

public static class QueryStringBuilderExtensions
{
    public static QueryStringBuilder AppendSearchParameters( this QueryStringBuilder builder, SearchStationsParameters parameters )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( parameters );

        return builder.Append( nameof( parameters.Codec ), ( int? )parameters.Codec )
            .Append( nameof( parameters.Count ), parameters.Count )
            .Append( nameof( parameters.CountryCode ), parameters.CountryCode )
            .Append( nameof( parameters.HasLocation ), parameters.HasLocation )
            .Append( nameof( parameters.LanguageCode ), parameters.LanguageCode )
            .Append( nameof( parameters.Name ), parameters.Name )
            .Append( nameof( parameters.Offset ), parameters.Offset )
            .Append( nameof( parameters.Order ), ( int? )parameters.Order )
            .Append( nameof( parameters.Proximity ), parameters.Proximity?.ToString() )
            .Append( nameof( parameters.Reverse ), parameters.Reverse )
            .Append( nameof( parameters.Tag ), parameters.Tag )
            .Append( nameof( parameters.Tags ), parameters.Tags );
    }
}