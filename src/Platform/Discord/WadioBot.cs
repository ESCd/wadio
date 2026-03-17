using NetCord;
using NetCord.Gateway;

namespace Wadio.Platform.Discord;

internal sealed class WadioBot( GatewayClient gateway ) : BackgroundService
{
    protected override Task ExecuteAsync( CancellationToken cancellation )
    {
        gateway.Ready += e => gateway.UpdatePresenceAsync( new( UserStatusType.Online )
        {
            Activities = [ new( "i'm all ears...", UserActivityType.Listening )
            {
                ApplicationId = e.ApplicationId,
            }],
            Afk = false,
            Since = DateTimeOffset.UtcNow,
        } );

        return Task.CompletedTask;
    }
}