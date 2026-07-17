using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Client.Abstractions;

namespace Wadio.Platform.Sampler.Client;

public static class SamplerClientServiceExtensions
{
    public static IServiceCollection AddMetadataSampler( this IServiceCollection services, Action<IHttpClientBuilder>? configure = default )
    {
        ArgumentNullException.ThrowIfNull( services );

        var builder = services.AddHostedService<MetadataSamplePublisher>()
            .AddSingleton( Channel.CreateBounded<MetadataSample>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            } ) )
            .AddTransient<IMetadataSampler, MetadataSampler>()
            .AddHttpClient( nameof( MetadataSample ) )
            .ConfigureHttpClient( http =>
            {
                ArgumentNullException.ThrowIfNull( http );

                http.BaseAddress = new Uri( "https+http://sampler/ingest/metadata" );
                http.DefaultRequestHeaders.UserAgent.Add( new( "Wadio.Platform.Sampler.Client", WadioVersion.Current ) );
                http.DefaultRequestVersion = HttpVersion.Version30;
            } );

        builder.AddStandardResilienceHandler();
        configure?.Invoke( builder );

        return services;
    }
}