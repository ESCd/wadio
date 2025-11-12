using System.Net.Mime;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Json;

namespace Wadio.App.Web.Configuration;

internal sealed class ConfigureOpenApi : IPostConfigureOptions<OpenApiOptions>
{
    public void PostConfigure( string? _, OpenApiOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        options.AddOperationTransformer<DeprecatedTransformer>()
            .AddOperationTransformer<SecuritySchemeTransformer>()
            .AddNullableTransformer()
            .AddProblemResponseTransformer()
            .AddDocumentTransformer( ( document, _, _ ) =>
            {
                document.Info = new()
                {
                    Title = "Wadio.App.Web API",
                    Version = WadioVersion.Current,
                };

                return Task.CompletedTask;
            } )
            .AddOperationTransformer( ( operation, _, _ ) =>
            {
                var parameters = operation.Parameters?.Where( parameter => parameter.In is ParameterLocation.Query );
                if( parameters is not null )
                {
                    foreach( var parameter in parameters )
                    {
                        parameter.Name = JsonPathNamingPolicy.CamelCase.ConvertName( parameter.Name );
                    }
                }

                return Task.CompletedTask;
            } ); ;
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

internal static partial class NullableTransformer
{
    internal sealed partial class ChainedDelegate( Func<JsonTypeInfo, string?> next )
    {
        [GeneratedRegex( "^NullableOf" )]
        private partial Regex NullableRegex { get; }
        private readonly Type typeOfNullable = typeof( Nullable<> );

        public string? Invoke( JsonTypeInfo type )
        {
            var result = next( type );

            // remove the "NullableOf" prefix for nullable types if present
            if( result is not null && type.Type.IsGenericType && type.Type.GetGenericTypeDefinition() == typeOfNullable )
            {
                result = NullableRegex.Replace( result, "" );
            }

            return result;
        }
    }

    public static OpenApiOptions AddNullableTransformer( this OpenApiOptions options )
    {
        options.AddSchemaTransformer( ( schema, context, _ ) =>
        {
            if( schema.Properties is not null )
            {
                foreach( var property in schema.Properties )
                {
                    if( schema.Required?.Contains( property.Key ) is not true )
                    {
                        property.Value.Nullable = false;
                    }

                    // Also need to remove `null` from enum values if present
                    if( property.Value.Enum is not null )
                    {
                        property.Value.Enum = [ .. property.Value.Enum.Where( e => e is OpenApiString { Value: not null } ) ];
                    }

                    // And remove default value of null if set
                    if( property.Value.Default is OpenApiNull )
                    {
                        property.Value.Default = null;
                    }
                }
            }

            return Task.CompletedTask;
        } );

        var reference = new ChainedDelegate( options.CreateSchemaReferenceId );
        options.CreateSchemaReferenceId = reference.Invoke;

        return options;
    }
}

static file class ProblemResponseTransformer
{
    public static OpenApiOptions AddProblemResponseTransformer( this OpenApiOptions options )
    {
        options.AddDocumentTransformer( ( document, _, _ ) =>
        {
            document.Components ??= new();
            document.Components.Responses ??= new Dictionary<string, OpenApiResponse>();
            document.Components.Responses[ "Problem" ] = new()
            {
                Description = "Problem",
                Content = new Dictionary<string, OpenApiMediaType>()
                {
                    [ MediaTypeNames.Application.Json ] = new()
                    {
                        Schema = new()
                        {
                            Reference = new()
                            {
                                Type = ReferenceType.Schema,
                                Id = nameof( HttpValidationProblemDetails )
                            }
                        }
                    },
                    [ MediaTypeNames.Application.ProblemJson ] = new()
                    {
                        Schema = new()
                        {
                            Reference = new()
                            {
                                Type = ReferenceType.Schema,
                                Id = nameof( HttpValidationProblemDetails )
                            }
                        }
                    },
                }
            };

            return Task.CompletedTask;
        } );

        options.AddOperationTransformer( ( operation, _, _ ) =>
        {
            var responses = new OpenApiResponses( operation.Responses )
            {
                [ "4XX" ] = new()
                {
                    Reference = new()
                    {
                        Type = ReferenceType.Response,
                        Id = "Problem"
                    }
                },
                [ "5XX" ] = new()
                {
                    Reference = new()
                    {
                        Type = ReferenceType.Response,
                        Id = "Problem"
                    }
                }
            };

            foreach( var entry in responses )
            {
                if( int.TryParse( entry.Key, out var code ) )
                {
                    if( code is StatusCodes.Status400BadRequest )
                    {
                        if( entry.Value.Content.TryGetValue( MediaTypeNames.Application.Json, out var type ) )
                        {
                            type.Schema = new()
                            {
                                Reference = new()
                                {
                                    Type = ReferenceType.Schema,
                                    Id = nameof( HttpValidationProblemDetails )
                                }
                            };
                        }

                        if( entry.Value.Content.TryGetValue( MediaTypeNames.Application.ProblemJson, out type ) )
                        {
                            type.Schema = new()
                            {
                                Reference = new()
                                {
                                    Type = ReferenceType.Schema,
                                    Id = nameof( HttpValidationProblemDetails )
                                }
                            };
                        }
                    }

                    if( entry.Value.Content.TryGetValue( MediaTypeNames.Application.Json, out var json ) && json.Schema is null )
                    {
                        if( entry.Value.Content.TryGetValue( MediaTypeNames.Application.ProblemJson, out var problem ) )
                        {
                            json.Schema = problem.Schema;
                        }
                    }
                }
            }

            operation.Responses = responses;
            return Task.CompletedTask;
        } );

        return options;
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
                [ new()
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