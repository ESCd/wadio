using Microsoft.AspNetCore.Components.Routing;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components.Layout;

internal sealed record NavigationItem( IconName Icon, string Label, string Path )
{
    public DOMBreakpoint? Breakpoint { get; init; }
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
}

internal static class NavigationItems
{
    public static readonly NavigationItem[] Values = [
        new(IconName.Search, "Search", "/search")];
}