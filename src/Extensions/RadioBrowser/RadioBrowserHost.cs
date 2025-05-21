using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser;

public readonly struct RadioBrowserHost
{
    [JsonConverter( typeof( IPAddressConverter ) )]
    public readonly IPAddress Address { get; }
    public readonly long Latency { get; }
    public readonly string? Name { get; }

    private RadioBrowserHost( IPAddress address, long latency, string? name = null )
    {
        Address = address;
        Latency = latency;
        Name = name;
    }

    [UnsupportedOSPlatform( "browser" )]
    public static async IAsyncEnumerable<RadioBrowserHost> Resolve( [EnumeratorCancellation] CancellationToken cancellation = default )
    {
        using var ping = new Ping();
        foreach( var address in await Dns.GetHostAddressesAsync( "all.api.radio-browser.info", cancellation ) )
        {
            var reply = await Ping( ping, address, cancellation );
            if( reply.Status is not IPStatus.Success )
            {
                continue;
            }

            var entry = await Dns.GetHostEntryAsync( address );
            yield return new( address, reply.RoundtripTime, entry?.HostName );
        }
    }

    [UnsupportedOSPlatform( "browser" )]
    public static async ValueTask<RadioBrowserHost> ResolveEffective( CancellationToken cancellation = default )
    {
        PingReply? target = default;
        using( var ping = new Ping() )
        {
            foreach( var address in await Dns.GetHostAddressesAsync( "all.api.radio-browser.info", cancellation ) )
            {
                var reply = await Ping( ping, address, cancellation );
                if( reply.Status is not IPStatus.Success )
                {
                    continue;
                }

                if( target is null || reply.RoundtripTime < target.RoundtripTime )
                {
                    target = reply;
                }
            }
        }

        if( target is null )
        {
            throw new InvalidOperationException( $"Failed to resolve a {nameof( RadioBrowserHost )}." );
        }

        var entry = await Dns.GetHostEntryAsync( target.Address );
        return new( target.Address, target.RoundtripTime, entry?.HostName );
    }

    private static Task<PingReply> Ping( Ping ping, IPAddress address, CancellationToken cancellation )
        => ping.SendPingAsync(
            address,
            TimeSpan.FromSeconds( 2.5 ),
            cancellationToken: cancellation );
}