using Microsoft.AspNetCore.Components;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Components.Routing;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components.Layout;

public sealed record AppLayoutState : State<AppLayoutState>
{
    public bool IsMenuOpen { get; init; }

    internal static async ValueTask<AppLayoutState> GoToRandom( IWadioApi api, NavigationManager navigation, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( navigation );
        ArgumentNullException.ThrowIfNull( state );

        var station = await api.Stations.Random();
        if( station is not null )
        {
            navigation.NavigateToStation( station.Id );
        }

        return state;
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