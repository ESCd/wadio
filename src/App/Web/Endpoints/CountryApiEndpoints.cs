using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.Web.Endpoints;

internal static class CountryApiEndpoints
{
    public static Ok<IAsyncEnumerable<Country>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Countries.Get( cancellation ) );
}