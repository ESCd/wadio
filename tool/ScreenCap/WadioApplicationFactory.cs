using Microsoft.AspNetCore.Mvc.Testing;

namespace ScreenCap;

internal sealed class WadioApplicationFactory : WebApplicationFactory<Wadio.App.Web.Program>
{
    public WadioApplicationFactory( )
    {
        ClientOptions.BaseAddress = new Uri( "https://localhost:5001" );
        UseKestrel( options =>
        {
            options.ConfigureHttpsDefaults( https => https.AllowAnyClientCertificate() );

            options.ListenLocalhost( 5000 );
            options.ListenLocalhost( 5001, options => options.UseHttps() );
        } );
    }
}