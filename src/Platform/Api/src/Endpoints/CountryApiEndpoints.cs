using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Endpoints;

internal static class CountryApiEndpoints
{
    public static Ok<IAsyncEnumerable<Country>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Countries.Get( cancellation ) );
}