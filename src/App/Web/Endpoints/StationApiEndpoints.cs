using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.Abstractions.Api;
using Wadio.App.Web.Infrastructure;

namespace Wadio.App.Web.Endpoints;

internal static class StationApiEndpoints
{
    public static async Task<Results<Ok<Station>, NotFound>> Get(
        [FromServices] IWadioApi api,
        [FromRoute] Guid stationId,
        CancellationToken cancellation )
    {
        var station = await api.Stations.Get( stationId, cancellation );
        if( station is null )
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok( station );
    }

    public static async Task<Ok<Station>> Random(
        [FromServices] IWadioApi api,
        [AsParameters] SearchStationsParameters parameters,
        CancellationToken cancellation )
        => TypedResults.Ok( await api.Stations.Random( parameters, cancellation ) );

    public static Results<Ok<IAsyncEnumerable<Station>>, ValidationProblem> Search(
        [FromServices] IWadioApi api,
        [AsParameters] SearchStationsParameters parameters,
        CancellationToken cancellation )
    {
        if( !Validation.TryValidate( parameters, out var errors ) )
        {
            return TypedResults.ValidationProblem( errors );
        }

        return TypedResults.Ok( api.Stations.Search( parameters, cancellation ) );
    }

    public static async Task<Ok<bool>> Track( [FromServices] IWadioApi api, [FromRoute] Guid stationId, CancellationToken cancellation )
        => TypedResults.Ok( await api.Stations.Track( stationId, cancellation ) );

    public static async Task<Ok<bool>> Vote( [FromServices] IWadioApi api, [FromRoute] Guid stationId, CancellationToken cancellation )
        => TypedResults.Ok( await api.Stations.Vote( stationId, cancellation ) );
}