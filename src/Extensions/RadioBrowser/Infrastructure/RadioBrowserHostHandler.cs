using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class RadioBrowserHostHandler( IRadioBrowserHostResolver resolver ) : DelegatingHandler
{
    public const string Authority = "rdb://radio.browser";

    protected sealed override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellation )
    {
        if( request.RequestUri?.GetLeftPart( UriPartial.Authority ) is Authority )
        {
            var host = await resolver.Resolve( cancellation );
            request.RequestUri = host.BuildUrl( request.RequestUri );
        }

        return await base.SendAsync( request, cancellation );
    }
}

