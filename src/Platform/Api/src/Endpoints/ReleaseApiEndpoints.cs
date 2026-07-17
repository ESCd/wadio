using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Endpoints;

internal static class ReleaseApiEndpoints
{
    public static Ok<IAsyncEnumerable<Release>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Releases.Get( cancellation ) );
}