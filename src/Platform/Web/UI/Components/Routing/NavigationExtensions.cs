using Microsoft.AspNetCore.Components;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Web.UI.Components.Routing;

internal static class NavigationExtensions
{
    public static Uri GetStationIconUrl( this NavigationManager navigation, Guid stationId )
    {
        ArgumentNullException.ThrowIfNull( navigation );

        return navigation.ToAbsoluteUri( $"/ico/station/{stationId}" );
    }

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

        var url = navigation.ToAbsoluteUri( $"/station/{stationId}" );
        navigation.NavigateTo( url.AbsoluteUri );
    }
}