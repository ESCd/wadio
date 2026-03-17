using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Endpoints;

internal static class ApiEndpoints
{
    public static async Task<Ok<WadioVersion>> Version( [FromServices] IWadioApi api )
        => TypedResults.Ok( await api.Version() );
}