using System.Net;
using Wadio.Platform.Api.Abstractions;
using Yarp.ReverseProxy.Forwarder;

namespace Wadio.Platform.Router.Infrastructure;

internal sealed class StationIconForwarder( IHttpForwarder forwarder ) : IDisposable
{
    private readonly HttpMessageInvoker invoker = new( new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        ConnectTimeout = TimeSpan.FromSeconds( 15 ),
        UseCookies = false,
        UseProxy = false,
    } );

    public void Dispose( ) => invoker.Dispose();

    public async Task<ForwarderError> SendAsync( HttpContext context, StationIco icon )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( icon );

        var url = icon.Upscale ?? icon.Public;

        return await forwarder.SendAsync(
            context,
            url.GetLeftPart( UriPartial.Authority ),
            invoker,
            new ForwarderRequestConfig(),
            new IconTransform( url ) );
    }

    private sealed class IconTransform( Uri url ) : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(
            HttpContext context,
            HttpRequestMessage proxied,
            string prefix,
            CancellationToken cancellation )
        {
            await base.TransformRequestAsync( context, proxied, prefix, cancellation );

            proxied.RequestUri = url;
            proxied.Headers.Host = default;
        }
    }
}