using Microsoft.AspNetCore.Components.Routing;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components.Layout;

internal static class NavigationItems
{
    public static readonly NavigationItem[] All = [
        new(IconName.Explore, "Explore", "/explore"),
        new(IconName.Search, "Search", "/search")];

    public static readonly NavigationItem Home = new( IconName.Home, "Discover", "/" )
    {
        Match = NavLinkMatch.All
    };

    public static readonly NavigationItem Random = new( new( IconName.Casino, true ), "Random", "#" )
    {
        Match = NavLinkMatch.All
    };
}

internal sealed record NavigationItem( NavigationIcon Icon, string Label, string Path )
{
    public DOMBreakpoint? Breakpoint { get; init; }
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
}

internal sealed record NavigationIcon( IconName Name, bool Filled = false )
{
    public static implicit operator NavigationIcon( IconName icon ) => new( icon );
}