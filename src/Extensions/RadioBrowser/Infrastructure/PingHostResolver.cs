using System.Net;
using System.Net.NetworkInformation;
using ESCd.Extensions.Caching.Abstractions;
using Wadio.Extensions.RadioBrowser.Abstractions;

namespace Wadio.Extensions.RadioBrowser.Infrastructure;

internal sealed class PingHostResolver( IAsyncCache cache ) : RadioBrowserHostResolver( cache )
{
    protected override async ValueTask<RadioBrowserHost?> OnResolveHost( CancellationToken cancellation )
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

        if( target is not null )
        {
            var entry = await Dns.GetHostEntryAsync( target.Address );
            return new()
            {
                Address = target.Address,
                Name = entry?.HostName
            };
        }

        return default;
    }

    private static Task<PingReply> Ping( Ping ping, IPAddress address, CancellationToken cancellation )
        => ping.SendPingAsync(
            address,
            TimeSpan.FromSeconds( 2.5 ),
            cancellationToken: cancellation );
}