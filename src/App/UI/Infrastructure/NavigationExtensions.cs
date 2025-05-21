using Microsoft.AspNetCore.Components;

namespace Wadio.App.UI.Infrastructure;

internal static class NavigationExtensions
{
    public static void NavigateToStation( this NavigationManager navigation, Guid stationId )
    {
        ArgumentNullException.ThrowIfNull( navigation );

        navigation.NavigateTo( $"/station/{stationId}" );
    }
}