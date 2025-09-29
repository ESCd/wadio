using System.Diagnostics;
using System.Net.Http.Json;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using Wadio.Extensions.RadioBrowser.Abstractions;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class HttpHostResolver(
    IAsyncCache cache,
    HttpClient http,
    ILogger<HttpHostResolver> logger,
    IOptions<HttpHostResolverOptions> options ) : RadioBrowserHostResolver( cache )
{
    private readonly CacheKey cacheKey = new( nameof( HttpHostResolver ), Guid.NewGuid().ToString(), nameof( GetHostCandidates ) );

    public override async ValueTask DisposeAsync( )
    {
        await Cache.RemoveAsync( cacheKey );
        await base.DisposeAsync();
    }

    private async ValueTask<RadioBrowserHost[]> GetHostCandidates( CancellationToken cancellation ) => await Cache.GetOrCreateAsync<RadioBrowserHost[]>( cacheKey, async ( entry, cancellation ) =>
    {
        ArgumentNullException.ThrowIfNull( entry );

        entry.SetAbsoluteExpiration( TimeSpan.FromHours( 4 ) )
            .SetSlidingExpiration( TimeSpan.FromMinutes( 45 ) );

        var hosts = new HashSet<RadioBrowserHost>();
        foreach( var tracker in options.Value.TrackerUrls.Distinct() )
        {
            foreach( var host in await GetTrackerHosts( tracker, cancellation ).ConfigureAwait( false ) )
            {
                hosts.Add( host );
            }
        }

        return [ .. hosts.DistinctBy( host => host.Name ) ];
    }, cancellation ) ?? [];

    private async Task<RadioBrowserHost[]> GetTrackerHosts( Uri trackerUrl, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( http );
        ArgumentNullException.ThrowIfNull( trackerUrl );

        try
        {
            return await http.GetFromJsonAsync(
                new Uri( trackerUrl, "json/servers" ),
                RadioBrowserJsonContext.Default.RadioBrowserHostArray,
                cancellation ).ConfigureAwait( false ) ?? [];
        }
        catch( Exception e ) when( e is HttpRequestException or TimeoutRejectedException )
        {
            logger.TrackerFailed( e, trackerUrl );
            return [];
        }
    }

    protected override async ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation )
    {
        PingReply? result = default;
        foreach( var host in await GetHostCandidates( cancellation ).ConfigureAwait( false ) )
        {
            var reply = await Ping( host, cancellation ).ConfigureAwait( false );
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

    private async Task<PingReply> Ping( RadioBrowserHost host, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( host );

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await http.GetAsync(
                host.BuildUrl( new( RadioBrowserHostHandler.Authority ) ),
                cancellation ).ConfigureAwait( false );

            stopwatch.Stop();
            return new( stopwatch.Elapsed, host )
            {
                IsSuccess = response.IsSuccessStatusCode,
            };
        }
        catch( Exception e ) when( e is HttpRequestException or TimeoutRejectedException )
        {
            logger.HostFailed( e, host );

            stopwatch.Stop();
            return new( stopwatch.Elapsed, host )
            {
                IsSuccess = false,
            };
        }
    }

    private sealed record PingReply( TimeSpan Duration, RadioBrowserHost Host )
    {
        public bool IsSuccess { get; init; }
    }
}

internal static partial class HttpHostResolverLogging
{
    [LoggerMessage( -1, LogLevel.Warning, "Failed to reach tracker '{trackerUrl}'" )]
    public static partial void TrackerFailed( this ILogger<HttpHostResolver> logger, Exception e, Uri trackerUrl );

    [LoggerMessage( -2, LogLevel.Warning, "Failed to reach host: {host}" )]
    public static partial void HostFailed( this ILogger<HttpHostResolver> logger, Exception e, RadioBrowserHost host );
}

public sealed class HttpHostResolverOptions
{
    public HashSet<Uri> TrackerUrls { get; set; } = [ new( "https://all.api.radio-browser.info" ) ];
}
