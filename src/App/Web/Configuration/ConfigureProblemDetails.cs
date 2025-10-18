using Microsoft.Extensions.Options;
using Wadio.App.Abstractions;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure( ProblemDetailsOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        var customize = options.CustomizeProblemDetails;
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions.Add( "version", WadioVersion.Current );
            customize?.Invoke( context );
        };
    }
}