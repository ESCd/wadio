using System.Text.Json;
using Microsoft.Extensions.Options;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure( ProblemDetailsOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        var customize = options.CustomizeProblemDetails;
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions[ JsonNamingPolicy.CamelCase.ConvertName( nameof( ApiProblem.Version ) ) ] = WadioVersion.Current;
            customize?.Invoke( context );
        };
    }
}