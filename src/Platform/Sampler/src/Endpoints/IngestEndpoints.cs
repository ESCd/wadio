using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wadio.Platform.Hosting.Infrastructure;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Infrastructure;

namespace Wadio.Platform.Sampler.Endpoints;

internal static class IngestEndpoints
{
    public static async Task<Results<Ok, ValidationProblem>> Metadata(
        [FromServices] MetadataSampleWriter writer,
        [FromBody] MetadataSample sample,
        CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( writer );
        ArgumentNullException.ThrowIfNull( sample );

        if( !Validation.TryValidate( sample, out var errors ) )
        {
            return TypedResults.ValidationProblem( errors );
        }

        await writer.Write( sample, cancellation );
        return TypedResults.Ok();
    }
}