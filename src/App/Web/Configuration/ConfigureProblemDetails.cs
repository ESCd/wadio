using System.Text.Json;
using Microsoft.Extensions.Options;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure( ProblemDetailsOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        var customize = options.CustomizeProblemDetails;
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions.Add( JsonNamingPolicy.CamelCase.ConvertName( nameof( ApiProblem.Version ) ), WadioVersion.Current );
            customize?.Invoke( context );
        };
    }
}