using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Wadio.Platform.Abstractions;

namespace Wadio.Platform.Sampler.Configuration;

internal sealed class ConfigureOpenApi : IPostConfigureOptions<OpenApiOptions>
{
    public void PostConfigure( string? _, OpenApiOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

        options.AddDocumentTransformer<ServerUrlTransformer>()
            .AddDocumentTransformer( ( document, _, _ ) =>
            {
                document.Info = new()
                {
                    Title = "Wadio.Platform.Sampler",
                    Version = WadioVersion.Current,
                };

                return Task.CompletedTask;
            } )
            .AddOperationTransformer( ( operation, _, _ ) =>
            {
                operation.Parameters = operation.Parameters?.Select( parameter =>
                {
                    if( parameter.In is ParameterLocation.Query )
                    {

                        return new OpenApiParameter
                        {
#pragma warning disable CS0618
                            AllowEmptyValue = parameter.AllowEmptyValue,
#pragma warning restore CS0618
                            AllowReserved = parameter.AllowReserved,
                            Content = parameter.Content,
                            Deprecated = parameter.Deprecated,
                            Description = parameter.Description,
                            Example = parameter.Example,
                            Examples = parameter.Examples,
                            Explode = parameter.Explode,
                            Extensions = parameter.Extensions,
                            In = parameter.In,
                            Name = JsonNamingPolicy.CamelCase.ConvertName( parameter.Name! ),
                            Required = parameter.Required,
                            Schema = parameter.Schema?.CreateShallowCopy(),
                            Style = parameter.Style,
                        };

                    }

                    return parameter;
                } ).ToList();

                return Task.CompletedTask;
            } );
    }
}

sealed file class ServerUrlTransformer( IHttpContextAccessor contextAccessor ) : IOpenApiDocumentTransformer
{
    public Task TransformAsync( OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( document );
        ArgumentNullException.ThrowIfNull( context );

        var http = contextAccessor.HttpContext;
        if( http is null )
        {
            return Task.CompletedTask;
        }

        var request = http.Request;

        var path = request.PathBase.HasValue ? request.PathBase.Value.TrimEnd( '/' ) : string.Empty;
        document.Servers = [ new OpenApiServer
        {
            Url = $"{request.Scheme}://{request.Host}{path}"
        } ];

        return Task.CompletedTask;
    }
}