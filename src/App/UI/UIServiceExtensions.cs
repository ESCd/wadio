using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

        return services.AddScoped<ClipboardInterop>()
            .AddScoped<DialogInterop>()
            .AddScoped<DOMInterop>()
            .AddScoped<LocalStorageInterop>()
            .AddScoped<MapInterop>()
            .AddScoped<MarqueeInterop>()
            .AddScoped<PlayerInterop>();
    }
}