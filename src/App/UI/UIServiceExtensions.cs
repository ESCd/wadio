using System.Diagnostics.CodeAnalysis;
using Wadio.App.UI.Infrastructure;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI;

/// <summary> Extensions for registering services required by Wadio Components. </summary>
public static class UIServiceExtensions
{
    /// <summary> Register service required by Wadio Components. </summary>
    [DynamicDependency( DynamicallyAccessedMemberTypes.All, typeof( AppRoot ) )]
    public static IServiceCollection AddWadioUI( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull( services );

        // services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        // services.TryAddSingleton(
        //     static serviceProvider => serviceProvider.GetRequiredService<ObjectPoolProvider>()
        //         .CreateStringBuilderPool() );

        return services.AddScoped<IMessenger, Messenger>()
            .AddScoped<ClipboardInterop>()
            .AddScoped<DialogInterop>()
            .AddScoped<LocalStorageInterop>()
            .AddScoped<PlayerInterop>();
    }
}