using Microsoft.AspNetCore.Http.HttpResults;
using Wadio.App.Abstractions;

namespace Wadio.App.Web.Endpoints;

internal static class ApiEndpoints
{
    public static Ok<AppVersion> Version( ) => TypedResults.Ok( AppVersion.Value );
}