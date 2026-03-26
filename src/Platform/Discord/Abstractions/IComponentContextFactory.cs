using System.Diagnostics.CodeAnalysis;
using System.Text;
using ESCd.Extensions.Http;
using Microsoft.Extensions.ObjectPool;
using NetCord;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Client;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Hosting.Abstractions;

namespace Wadio.Platform.Discord.Abstractions;

internal interface IComponentContextFactory
{
    public ValueTask<ComponentCreationContext> Create( );
}

internal sealed record ComponentCreationContext(
    PlatformOptions Platform,
    ObjectPool<QueryStringBuilder> QueryStringBuilders,
    ObjectPool<StringBuilder> StringBuilders )
{
    [SuppressMessage( "Performance", "CA1822", Justification = "Method is instance-specific for future extensibility." )]
    public Color GetAccentColor( Station? station ) => WadioColor.Convert( station?.Id );

    public Uri CreateSearchUrl( SearchStationsParameters? parameters )
    {
        var url = new UriBuilder( new Uri( Platform.PublicUrl, "/search" ) )
        {
            Query = CreateQuery( QueryStringBuilders, parameters )
        };

        return url.Uri;

        static string CreateQuery( ObjectPool<QueryStringBuilder> queryStrings, SearchStationsParameters? parameters )
        {
            ArgumentNullException.ThrowIfNull( queryStrings );

            if( parameters is null )
            {
                return "";
            }

            var builder = queryStrings.Get();
            try
            {
                return builder.AppendSearchParameters( parameters ).ToString();
            }
            finally
            {
                queryStrings.Return( builder );
            }
        }
    }

    public Uri CreateStationUrl( Station station ) => new( Platform.PublicUrl, $"/stations/{station.Id}" );
}