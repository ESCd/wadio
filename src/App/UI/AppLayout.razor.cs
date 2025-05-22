using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Components;
using Wadio.App.UI.Infrastructure;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI;

public sealed record AppLayoutState : State
{
    public bool IsMenuOpen { get; init; }
    public bool IsRandomLoading { get; init; }

    internal static async IAsyncEnumerable<AppLayoutState> GoToRandom( IWadioApi api, NavigationManager navigation, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( navigation );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsRandomLoading = true
        });

        var station = await api.Stations.Random();
        if( station is not null )
        {
            navigation.NavigateToStation( station.Id );
        }

        yield return state with
        {
            IsRandomLoading = false,
        };
    }

    internal static async ValueTask<AppLayoutState> Load( LocalStorageInterop localStorage, AppLayoutState state )
    {
        var data = await localStorage.Get<MenuData>( "menu" );
        if( data is null )
        {
            return state;
        }

        return state with
        {
            IsMenuOpen = data?.IsOpen ?? false,
        };
    }

    internal static async IAsyncEnumerable<AppLayoutState> ToggleMenu( LocalStorageInterop localStorage, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( localStorage );
        ArgumentNullException.ThrowIfNull( state );

        yield return state with
        {
            IsMenuOpen = !state.IsMenuOpen,
        };

        await localStorage.Set<MenuData>( "menu", new( !state.IsMenuOpen ) );
    }
}

sealed file record MenuData( bool IsOpen = false );

internal sealed record NavigationItem( IconName Icon, string Label, string Path )
{
    public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;
}

internal static class NavigationItems
{
    public static readonly NavigationItem[] Values = [
        new(IconName.Explore, "Explore", "/")
    ];
}