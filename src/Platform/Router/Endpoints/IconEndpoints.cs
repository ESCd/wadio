using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Abstractions.Json;
using Wadio.Platform.Router.Infrastructure;

namespace Wadio.Platform.Router.Endpoints;

internal static class IconEndpoints
{
    public static async Task Station(
        HttpContext context,
        [FromServices] IWadioApi api,
        [FromServices] StationIconForwarder forwarder,
        [FromRoute] Guid stationId )
    {
        var (icon, problem) = await GetIcon( api, stationId, context.RequestAborted );
        if( icon is null || problem is not null )
        {
            context.Response.StatusCode = problem?.Status ?? StatusCodes.Status500InternalServerError;
            if( problem is not null )
            {
                await context.Response.WriteAsJsonAsync(
                    problem,
                    ApiJsonContext.Default.ApiProblem,
                    MediaTypeNames.Application.ProblemJson,
                    context.RequestAborted );
            }

            return;
        }

        await forwarder.SendAsync( context, icon );

        static async ValueTask<(StationIco? Icon, ApiProblem? Problem)> GetIcon( IWadioApi api, Guid stationId, CancellationToken cancellation )
        {
            try
            {
                return (await api.Stations.Ico( stationId, cancellation ), default);
            }
            catch( ApiProblemException problem )
            {
                return (default, problem.Problem);
            }
        }
    }
}