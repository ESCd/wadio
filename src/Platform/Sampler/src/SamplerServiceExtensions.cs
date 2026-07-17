using LiteDB.Extensions.DependencyInjection;
using Wadio.Platform.Hosting.Configuration;
using Wadio.Platform.Sampler.Configuration;
using Wadio.Platform.Sampler.Infrastructure;

namespace Wadio.Platform.Sampler;

internal static class SamplerServiceExtensions
{
    public static TBuilder WithSamplerApi<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Services.AddEndpointsApiExplorer()
            .AddCors()
            .AddHttpContextAccessor()
            .AddLiteDbContext<SamplerDbContext>()
            .AddOpenApi( "api" )
            .AddProblemDetails()
            .AddRequestDecompression()
            .AddRequestTimeouts()
            .AddResponseCompression()
            .AddRouting();

        builder.Services.AddSingleton<MetadataSampleWriter>()
            .AddHostedService( services => services.GetRequiredService<MetadataSampleWriter>() );

        builder.Services.ConfigureOptions<ConfigureForwardedHeaders>()
            .ConfigureOptions<ConfigureJson>()
            .ConfigureOptions<ConfigureOpenApi>()
            .ConfigureOptions<ConfigureScalar>();

        return builder;
    }
}