using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureRedisCache( IServiceProvider services ) : IConfigureOptions<RedisCacheOptions>
{
    public void Configure( RedisCacheOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.ConnectionMultiplexerFactory = ( ) =>
        {
            var muxer = services.GetRequiredService<IConnectionMultiplexer>();
            return Task.FromResult( muxer );
        };
    }
}