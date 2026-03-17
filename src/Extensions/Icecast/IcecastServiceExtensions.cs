using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Wadio.Extensions.Icecast;

public static class IcecastServiceExtensions
{
    public static IServiceCollection AddIcecastClient( this IServiceCollection services, Action<IHttpClientBuilder>? configure = default )
    {
        ArgumentNullException.ThrowIfNull( services );

        var builder = services.AddHttpClient<IcecastClient>( ConfigureHttp );
        builder.AddStandardResilienceHandler();

        configure?.Invoke( builder );
        return services;

        static void ConfigureHttp( HttpClient http )
        {
            http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
            http.DefaultRequestVersion = HttpVersion.Version30;
            http.Timeout = TimeSpan.FromSeconds( 60 );

            static ProductInfoHeaderValue UserAgent( )
            {
                var type = typeof( IcecastClient );
                return new( type.FullName!, type.Assembly.GetName().Version!.ToString() );
            }
        }
    }
}