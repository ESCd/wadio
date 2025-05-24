using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class HttpHostResolver(
    IMemoryCache cache,
    HttpClient http,
    IOptions<HttpHostResolverOptions> options ) : RadioBrowserHostResolver( cache )
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

    private async ValueTask<RadioBrowserHost[]> GetHostCandidates( CancellationToken cancellation )
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
                .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) );

            var hosts = new HashSet<RadioBrowserHost>();
            foreach( var tracker in options.Value.TrackerUrls )
            {
                foreach( var host in await GetTrackerHosts( http, tracker, cancellation ) )
                {
                    hosts.Add( host );
                }
            }

            entry.SetValue( value = [ .. hosts.DistinctBy( host => host.Name ) ] );
            return value!;
        }
        finally
        {
            locker.Release();
        }

        static async Task<RadioBrowserHost[]> GetTrackerHosts( HttpClient http, Uri trackerUrl, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( http );
            ArgumentNullException.ThrowIfNull( trackerUrl );

            try
            {
                return await http.GetFromJsonAsync(
                    new Uri( trackerUrl, "json/servers" ),
                    RadioBrowserJsonContext.Default.RadioBrowserHostArray,
                    cancellation ) ?? [];
            }
            catch( Exception e ) when( e is HttpRequestException or TimeoutRejectedException )
            {
                return [];
            }
        }
    }

    protected override async ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation )
    {
        HttpPingResult? result = default;
        foreach( var host in await GetHostCandidates( cancellation ) )
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

        return result?.Host;
    }
}

public sealed class HttpHostResolverOptions
{
    public HashSet<Uri> TrackerUrls { get; set; } = [ new( "https://all.api.radio-browser.info" ) ];
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
                host.BuildUrl( new( RadioBrowserHostHandler.Authority ) ),
                cancellation );

            return new( TimeSpan.FromTicks( DateTime.Now.Ticks - now ), host )
            {
                IsSuccess = response.IsSuccessStatusCode,
            };
        }
        catch( Exception e ) when( e is HttpRequestException or TimeoutRejectedException )
        {
            return new( TimeSpan.MaxValue, host )
            {
                IsSuccess = false,
            };
        }
    }
}
