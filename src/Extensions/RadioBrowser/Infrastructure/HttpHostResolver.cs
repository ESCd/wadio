using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.ChannelExtensions;
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

            return await options.Value.TrackerUrls.ToChannel( options.Value.TrackerUrls.Count, cancellationToken: cancellation )
                .PipeAsync(
                    Environment.ProcessorCount,
                    tracker => GetTrackerHosts( tracker, cancellation ),
                    cancellationToken: cancellation )
                .AsAsyncEnumerable( cancellation )
                .SelectMany( hosts => hosts.Distinct() )
                .Distinct()
                .ToArrayAsync( cancellation )
                .ConfigureAwait( false );

            async ValueTask<RadioBrowserHost[]> GetTrackerHosts( Uri trackerUrl, CancellationToken cancellation )
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
        }, cancellation ) ?? [];

    protected override async ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation )
    {
        var hosts = await GetHostCandidates( cancellation ).ConfigureAwait( false );
        return await hosts.ToChannel( hosts.Length, cancellationToken: cancellation )
            .PipeAsync(
                Environment.ProcessorCount,
                host => Ping( host, cancellation ),
                cancellationToken: cancellation )
            .Filter( reply => reply.IsSuccess )
            .AsAsyncEnumerable( cancellation )
            .OrderBy( reply => reply.Duration )
            .Take( 1 )
            .Select( reply => reply.Host )
            .FirstOrDefaultAsync( cancellation )
            .ConfigureAwait( false );

        async ValueTask<PingReply> Ping( RadioBrowserHost host, CancellationToken cancellation )
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
    }

    private sealed record PingReply( TimeSpan Duration, RadioBrowserHost Host )
    {
        public bool IsSuccess { get; init; }
    }
}

static file class AsyncEnumerableExtensions
{
    public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>( this IAsyncEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector )
        where TSource : IEnumerable<TResult>
    {
        ArgumentNullException.ThrowIfNull( source );
        ArgumentNullException.ThrowIfNull( selector );

        return source.SelectMany( value => Selector( value, selector ) );

        static async IAsyncEnumerable<TResult> Selector(
            TSource source,
            Func<TSource, IEnumerable<TResult>> selector )
        {
            foreach( var result in selector( source ) )
            {
                yield return result;
            }
        }
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
