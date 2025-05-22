using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Polly.Timeout;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class HttpHostResolver( IMemoryCache cache, HttpClient http ) : RadioBrowserHostResolver( cache )
{
    private readonly object cacheKey = new();
    private readonly SemaphoreSlim locker = new( 1, 1 );

    protected override void Dispose( bool disposing )
    {
        if( disposing )
        {
            locker.Dispose();
            Cache.Remove( cacheKey );
        }

        base.Dispose( disposing );
    }

    private async ValueTask<RadioBrowserHost[]> GetHosts( CancellationToken cancellation )
    {
        if( Cache.TryGetValue<RadioBrowserHost[]>( cacheKey, out var value ) && value is not null )
        {
            return value;
        }

        await locker.WaitAsync( cancellation );
        try
        {
            using var entry = Cache.CreateEntry( cacheKey )
                .SetAbsoluteExpiration( TimeSpan.FromHours( 4 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) )
                .SetValue( value = await http.GetFromJsonAsync(
                    "https://all.api.radio-browser.info/json/servers",
                    RadioBrowserJsonContext.Default.RadioBrowserHostArray,
                    cancellation ) );

            return value!;
        }
        finally
        {
            locker.Release();
        }
    }

    protected override async ValueTask<RadioBrowserHost> OnResolveHost( CancellationToken cancellation )
    {
        HttpPingResult? result = default;
        foreach( var host in await GetHosts( cancellation ) )
        {
            var reply = await HttpPingResult.Send( http, host, cancellation );
            if( !reply.IsSuccess )
            {
                continue;
            }

            if( result is null || reply.Duration < result.Duration )
            {
                result = reply;
            }
        }

        if( result is null )
        {
            throw new InvalidOperationException( $"Failed to resolve a {nameof( RadioBrowserHost )}." );
        }

        return result.Host;
    }
}

sealed file record HttpPingResult( TimeSpan Duration, RadioBrowserHost Host )
{
    public bool IsSuccess { get; init; }

    public static async ValueTask<HttpPingResult> Send( HttpClient http, RadioBrowserHost host, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( http );
        ArgumentNullException.ThrowIfNull( host );

        try
        {
            var now = DateTime.Now.Ticks;
            using var response = await http.GetAsync(
                host.BuildUrl( new( RadioBrowserHostHandler.Authority + "/json/servers" ) ),
                cancellation );

            return new( TimeSpan.FromTicks( DateTime.Now.Ticks - now ), host )
            {
                IsSuccess = response.IsSuccessStatusCode,
            };
        }
        catch( TimeoutRejectedException )
        {
            return new( TimeSpan.MaxValue, host )
            {
                IsSuccess = false,
            };
        }
    }
}
