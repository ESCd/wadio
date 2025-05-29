using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wadio.Extensions.Caching.Abstractions;

namespace Wadio.Extensions.Caching;

public static class AsyncCacheServiceExtensions
{
    public static IServiceCollection AddAsyncCache( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddMemoryCache()
            .TryAddSingleton<IAsyncCache, AsyncCache>();

        return services;
    }
}