using Microsoft.AspNetCore.Components;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Web.UI.Components.Routing;
using Wadio.Platform.Web.UI.Interop;

namespace Wadio.Platform.Web.UI.Components.Layout;

public sealed record AppLayoutState : State<AppLayoutState>
{
    public bool IsMenuOpen { get; init; }

    internal static async ValueTask<AppLayoutState> GoToRandom( IWadioApi api, NavigationManager navigation, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( navigation );
        ArgumentNullException.ThrowIfNull( state );

        var station = await api.Stations.Random();
        if( station is not null )
        {
            navigation.NavigateToStation( station.Id );
        }

        return state;
    }

    internal static async ValueTask<AppLayoutState> Load( DOMInterop dom, LocalStorageInterop localStorage, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( dom );
        ArgumentNullException.ThrowIfNull( localStorage );
        ArgumentNullException.ThrowIfNull( state );

        if( await dom.GetActiveBreakpoint() < DOMBreakpoint.Medium )
        {
            return state with
            {
                IsMenuOpen = true,
            };
        }

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

    internal static AppLayoutState OnBreakpointChange( BreakpointChangeEventArgs e, AppLayoutState state )
    {
        ArgumentNullException.ThrowIfNull( e );
        ArgumentNullException.ThrowIfNull( state );

        if( e.From >= DOMBreakpoint.Medium && e.To < DOMBreakpoint.Medium )
        {
            // NOTE: force menu visibility on small viewports (bottom bar navigation)
            return state with
            {
                IsMenuOpen = true,
            };
        }

        if( e.From < DOMBreakpoint.Medium && e.To >= DOMBreakpoint.Medium )
        {
            // NOTE: auto-close the menu when transitioning to larger viewports
            return state with
            {
                IsMenuOpen = false,
            };
        }

        return state;
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