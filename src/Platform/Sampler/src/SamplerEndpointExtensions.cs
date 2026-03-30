using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Sampler.Endpoints;

namespace Wadio.Platform.Sampler;

public static class SamplerEndpointExtensions
{
    public static RouteGroupBuilder MapSamplerApi( this WebApplication app, string prefix = "/api" )
    {
        ArgumentNullException.ThrowIfNull( app );
        ArgumentException.ThrowIfNullOrWhiteSpace( prefix );

        var api = app.MapGroup( prefix )
            .ProducesValidationProblem()
            .WithMetadata( new ApiControllerAttribute() );

        api.MapPost( "/ingest/metadata", IngestEndpoints.Metadata );
        return api;
    }
}