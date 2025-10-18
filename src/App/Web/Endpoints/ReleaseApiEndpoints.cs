using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.Web.Endpoints;

internal static class ReleaseApiEndpoints
{
    public static Ok<IAsyncEnumerable<Release>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Releases.Get( cancellation ) );
}