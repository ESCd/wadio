using System.Text.Json;
using Microsoft.Extensions.Options;
using Wadio.Platform.Sampler.Abstractions.Json;

namespace Wadio.Platform.Sampler.Configuration;

internal sealed class ConfigureJson : IConfigureOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>, IConfigureOptions<Microsoft.AspNetCore.Mvc.JsonOptions>, IConfigureOptions<Microsoft.AspNetCore.SignalR.JsonHubProtocolOptions>
{
    public void Configure( Microsoft.AspNetCore.Http.Json.JsonOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );
        AddAppJson( options.SerializerOptions );
    }

    public void Configure( Microsoft.AspNetCore.Mvc.JsonOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );
        AddAppJson( options.JsonSerializerOptions );
    }

    public void Configure( Microsoft.AspNetCore.SignalR.JsonHubProtocolOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );
        AddAppJson( options.PayloadSerializerOptions );
    }

    private static void AddAppJson( JsonSerializerOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        options.TypeInfoResolverChain.Insert( 0, SamplerJsonContext.Default );
    }
}