using Microsoft.AspNetCore.Components;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.UI.Components.Routing;

internal static class NavigationExtensions
{
    public static void NavigateToSearch( this NavigationManager navigation, SearchStationsParameters? parameters = default, bool replace = false )
    {
        ArgumentNullException.ThrowIfNull( navigation );

        var url = navigation.GetUriWithQueryParameters( "/search", new Dictionary<string, object?>
        {
            { nameof(Pages.Search.Country), parameters?.CountryCode },
            { nameof(Pages.Search.Language), parameters?.LanguageCode },
            { nameof(Pages.Search.Name), parameters?.Name },
            { nameof(Pages.Search.Order), (int?)parameters?.Order },
            { nameof(Pages.Search.Tag), parameters?.Tag },
        } );

        navigation.NavigateTo( url, replace: replace );
    }

    public static void NavigateToStation( this NavigationManager navigation, Guid stationId )
    {
        ArgumentNullException.ThrowIfNull( navigation );

        navigation.NavigateTo( $"/station/{stationId}" );
    }
}