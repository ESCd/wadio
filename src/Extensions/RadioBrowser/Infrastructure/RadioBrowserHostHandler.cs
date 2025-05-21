using Microsoft.Extensions.Caching.Memory;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class RadioBrowserHostHandler( IMemoryCache cache ) : DelegatingHandler
{
    public const string Authority = "rdb://radio.browser";

    private readonly object cacheKey = new();
    private readonly SemaphoreSlim locker = new( 1, 1 );

    protected override void Dispose( bool disposing )
    {
        if( disposing )
        {
            locker.Dispose();
            cache.Remove( cacheKey );
        }

        base.Dispose( disposing );
    }

    private async ValueTask<RadioBrowserHost> ResolveHost( CancellationToken cancellation )
    {
        if( cache.TryGetValue( cacheKey, out var value ) && value is RadioBrowserHost host )
        {
            return host;
        }

        await locker.WaitAsync( cancellation );
        try
        {
            using var entry = cache.CreateEntry( cacheKey )
                .SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) )
                .SetValue( host = await RadioBrowserHost.ResolveEffective( cancellation ) );
        }
        finally
        {
            locker.Release();
        }

        return host;
    }

    protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellation )
    {
        if( request.RequestUri?.GetLeftPart( UriPartial.Authority ) is Authority )
        {
            var host = await ResolveHost( cancellation );
            request.RequestUri = new UriBuilder( request.RequestUri )
            {
                Scheme = "https://",
                Host = string.IsNullOrEmpty( host.Name ) ? host.Address.ToString() : host.Name,
                Path = $"/json{request.RequestUri.AbsolutePath}"
            }.Uri;
        }

        return await base.SendAsync( request, cancellation );
    }
}