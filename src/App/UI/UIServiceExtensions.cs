using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ESCd.Extensions.Caching;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Wadio.App.UI.Infrastructure;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI;

/// <summary> Extensions for registering services required by Wadio Components. </summary>
public static class UIServiceExtensions
{
    /// <summary> Register service required by Wadio Components. </summary>
    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( AppRoot ) )]
    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( ImmutableArray<> ) )]
    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( ImmutableDictionary<,> ) )]
    public static IServiceCollection AddWadioUI( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        services.AddAsyncCache();

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton( serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().CreateHashSetPool<string>() );
        services.TryAddSingleton( serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().CreateHashSetPool<Guid>() );
        services.TryAddSingleton( serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().CreateHashSetPool<Uri>() );
        services.TryAddSingleton( serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>().CreateStringBuilderPool() );

        return services.AddScoped<ClipboardInterop>()
            .AddScoped<DialogInterop>()
            .AddScoped<DOMInterop>()
            .AddScoped<GeolocationInterop>()
            .AddScoped<HistoryInterop>()
            .AddScoped<KeyboardInterop>()
            .AddScoped<LocalStorageInterop>()
            .AddScoped<MapInterop>()
            .AddScoped<MarqueeInterop>()
            .AddScoped<PlayerInterop>()
            .AddScoped<QuickSearchInterop>();
    }
}