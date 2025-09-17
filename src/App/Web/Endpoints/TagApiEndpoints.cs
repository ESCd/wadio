using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.Web.Endpoints;

internal static class TagApiEndpoints
{
    public static Ok<IAsyncEnumerable<Tag>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Tags.Get( cancellation ) );
}