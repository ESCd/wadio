using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class RadioBrowserHostHandler( IEnumerable<IRadioBrowserHostResolver> resolvers ) : DelegatingHandler
{
    public const string Authority = "rdb://radio.browser";

    protected sealed override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellation )
    {
        if( request.RequestUri?.GetLeftPart( UriPartial.Authority ) is Authority )
        {
            var host = await Resolve( cancellation ).ConfigureAwait( false );
            request.RequestUri = host.BuildUrl( request.RequestUri );
        }

        return await base.SendAsync( request, cancellation ).ConfigureAwait( false );
    }

    private async ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation )
    {
        var exceptions = new List<Exception>();
        foreach( var resolver in resolvers )
        {
            try
            {
                var host = await resolver.Resolve( cancellation ).ConfigureAwait( false );
                if( host is not null )
                {
                    return host;
                }
            }
            catch( Exception exception )
            {
                exceptions.Add( exception );
            }
        }

        throw new AggregateException( $"A {nameof( RadioBrowserHost )} could not be resovled.", exceptions );
    }
}