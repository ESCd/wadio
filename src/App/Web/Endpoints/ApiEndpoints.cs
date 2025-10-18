using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.Web.Endpoints;

internal static class ApiEndpoints
{
    public static async Task<Ok<WadioVersion>> Version( [FromServices] IWadioApi api )
        => TypedResults.Ok( await api.Version() );
}