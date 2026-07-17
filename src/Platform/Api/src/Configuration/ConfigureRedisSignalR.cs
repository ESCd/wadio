using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureRedisSignalR( IServiceProvider services ) : IConfigureOptions<RedisOptions>
{
    public void Configure( RedisOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.Configuration.ChannelPrefix = RedisChannel.Literal( "Wadio.Platform.Api.Signals" );
        options.ConnectionFactory = writer =>
        {
            var muxer = services.GetRequiredService<IConnectionMultiplexer>();
            return Task.FromResult( muxer );
        };
    }
}