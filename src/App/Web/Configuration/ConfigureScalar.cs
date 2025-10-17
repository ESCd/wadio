using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureScalar : IConfigureOptions<ScalarOptions>
{
    public void Configure( ScalarOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddDocument( "api", isDefault: true )
            .HideClientButton();
    }
}