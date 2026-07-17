using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure.Playback;
using Wadio.Platform.Hosting.Abstractions;

namespace Wadio.Platform.Discord;

internal sealed class WadioBot(
    GatewayClient gateway,
    IOptionsMonitor<PlatformOptions> platform,
    StationPlayerContext stationPlayer ) : BackgroundService
{
    protected override Task ExecuteAsync( CancellationToken cancellation )
    {
        gateway.Ready += async e =>
        {
            ArgumentNullException.ThrowIfNull( e );

            using var timer = new PeriodicTimer( TimeSpan.FromMinutes( 5 ) );
            do
            {
                var counts = new
                {
                    Guilds = await GetGuildCount( gateway.Rest, cancellation ),
                    Stations = stationPlayer.Count,
                };

                await gateway.UpdatePresenceAsync( new( UserStatusType.Online )
                {
                    Activities = [new($"{counts.Stations} Station{(counts.Stations == 1 ? "" : "s")} in {counts.Guilds} Guild{(counts.Guilds == 1 ? "" : "s")}", UserActivityType.Listening)
                    {
                        ApplicationId = e.ApplicationId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        Details = WadioEmoji.MonkeyAtPeace.ToString(),
                        Emoji = WadioEmoji.MonkeyAtPeace,
                        Url = platform.CurrentValue.PublicUrl.AbsoluteUri,
                    }],
                    Afk = false,
                    Since = DateTimeOffset.UtcNow,
                } );
            } while( await timer.WaitForNextTickAsync( cancellation ) );

            static async Task<int> GetGuildCount( RestClient client, CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( client );

                var app = await client.GetCurrentBotApplicationInformationAsync(
                    default,
                    cancellation );

                return app.ApproximateGuildCount ?? 0;
            }
        };

        return Task.CompletedTask;
    }
}