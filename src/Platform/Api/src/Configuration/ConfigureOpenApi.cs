using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Abstractions.Json;

namespace Wadio.Platform.Api.Configuration;

internal sealed class ConfigureOpenApi : IPostConfigureOptions<OpenApiOptions>
{
    public void PostConfigure( string? _, OpenApiOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddOperationTransformer<DeprecatedTransformer>()
            .AddOperationTransformer<SecuritySchemeTransformer>()
            .AddDocumentTransformer<ServerUrlTransformer>()
            .AddDocumentTransformer( ( document, _, _ ) =>
            {
                // document.Components ??= new();
                // document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                // foreach( var scheme in AuthenticationSchemes )
                // {
                // 	document.Components.SecuritySchemes.Add( scheme.Scheme, scheme );
                // }

                document.Info = new()
                {
                    Title = "Wadio.Platform.Api",
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
                            Name = JsonPathNamingPolicy.CamelCase.ConvertName( parameter.Name! ),
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

sealed file class DeprecatedTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync( OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( operation );
        ArgumentNullException.ThrowIfNull( context );

        if( context.Description.ActionDescriptor.EndpointMetadata.Any( metadata => metadata is ObsoleteAttribute ) )
        {
            operation.Deprecated = true;
        }

        return Task.CompletedTask;
    }
}

sealed file class SecuritySchemeTransformer( IAuthorizationPolicyProvider authorizationPolicyProvider ) : IOpenApiOperationTransformer
{
    public async Task TransformAsync( OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( operation );
        ArgumentNullException.ThrowIfNull( context );

        await foreach( var scheme in EnumerateSchemes( context.Description.ActionDescriptor ).Distinct() )
        {
            operation.Security ??= [];
            operation.Security.Add( new()
            {
                [ new OpenApiSecuritySchemeReference( scheme )
                {
                    Reference = new()
                    {
                        Id = scheme,
                        Type = ReferenceType.SecurityScheme,
                    }
                } ] = []
            } );
        }

        if( context.Description.ActionDescriptor.EndpointMetadata.Any( metadata => metadata is IAllowAnonymous ) )
        {
            operation.Security ??= [];
            operation.Security.Add( [] );
        }
    }

    private async IAsyncEnumerable<string> EnumerateSchemes( ActionDescriptor descriptor )
    {
        ArgumentNullException.ThrowIfNull( descriptor );

        foreach( var data in descriptor.EndpointMetadata.OfType<IAuthorizeData>() )
        {
            if( !string.IsNullOrEmpty( data.AuthenticationSchemes ) )
            {
                foreach( var scheme in data.AuthenticationSchemes.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries ) )
                {
                    yield return scheme;
                }
            }

            if( !string.IsNullOrEmpty( data.Policy ) )
            {
                var policy = await authorizationPolicyProvider.GetPolicyAsync( data.Policy );
                if( policy is not null )
                {
                    foreach( var scheme in policy.AuthenticationSchemes )
                    {
                        yield return scheme;
                    }
                }
            }
        }
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