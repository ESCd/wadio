using Microsoft.AspNetCore.Components.Routing;

namespace Wadio.App.UI.Components.Layout;

internal sealed record NavigationItem( IconName Icon, string Label, string Path )
{
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
}

internal static class NavigationItems
{
    public static readonly NavigationItem[] Values = [
        new(IconName.Explore, "Explore", "/")
        {
            Match = NavLinkMatch.All,
        },

        new(IconName.Search, "Search", "/search")];
}