using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureScalar : IConfigureOptions<ScalarOptions>
{
    public void Configure( ScalarOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddDocument( "api", isDefault: true )
            .HideClientButton()
            .WithDefaultHttpClient( ScalarTarget.Shell, ScalarClient.Curl )
            .WithTheme( ScalarTheme.Laserwave );
    }
}