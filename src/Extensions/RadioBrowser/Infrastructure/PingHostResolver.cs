using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Caching.Memory;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class PingHostResolver( IMemoryCache cache ) : RadioBrowserHostResolver( cache )
{
    protected override async ValueTask<RadioBrowserHost> OnResolveHost( CancellationToken cancellation )
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
        return new()
        {
            Address = target.Address,
            Name = entry?.HostName
        };
    }

    private static Task<PingReply> Ping( Ping ping, IPAddress address, CancellationToken cancellation )
        => ping.SendPingAsync(
            address,
            TimeSpan.FromSeconds( 2.5 ),
            cancellationToken: cancellation );
}