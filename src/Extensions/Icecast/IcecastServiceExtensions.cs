using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Wadio.Extensions.Icecast;

public static class IcecastServiceExtensions
{
    public static IServiceCollection AddIcecastClient( this IServiceCollection services, Action<IHttpClientBuilder>? configure = default )
    {
        ArgumentNullException.ThrowIfNull( services );

        var builder = services.AddHttpClient<IcecastMetadataClient>( ConfigureHttp );
        builder.AddStandardResilienceHandler();

        configure?.Invoke( builder );
        return services;

        static void ConfigureHttp( HttpClient http )
        {
            http.DefaultRequestHeaders.UserAgent.Add( UserAgent() );
            http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            http.Timeout = TimeSpan.FromSeconds( 60 );

            static ProductInfoHeaderValue UserAgent( )
            {
                var type = typeof( IcecastMetadataClient );
                return new( type.FullName!, type.Assembly.GetName().Version!.ToString() );
            }
        }
    }
}