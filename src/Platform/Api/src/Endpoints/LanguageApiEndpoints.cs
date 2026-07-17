using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Endpoints;

internal static class LanguageApiEndpoints
{
    public static Ok<IAsyncEnumerable<Language>> Get( [FromServices] IWadioApi api, CancellationToken cancellation )
        => TypedResults.Ok( api.Languages.Get( cancellation ) );
}