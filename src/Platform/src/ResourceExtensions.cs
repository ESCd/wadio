using Microsoft.Extensions.Hosting;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Hosting;

namespace Wadio.Platform;

internal static class ResourceExtensions
{
    public static IResourceBuilder<ContainerResource> AddCloudflared( this IDistributedApplicationBuilder builder, IResourceBuilder<ParameterResource> token )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( token );

        return builder.AddContainer( "cloudflared", "cloudflare/cloudflared", "latest" )
            .WithArgs( "tunnel", "--no-autoupdate", "run" )
            .WithEnvironment( "TUNNEL_TOKEN", token )
            .PublishAsDockerComposeService( ( _, service ) =>
            {
                ArgumentNullException.ThrowIfNull( service );

                service.Deploy = new()
                {
                    Placement = new()
                    {
                        Constraints = [ "node.labels.pool == manager" ]
                    },
                    Replicas = service.Deploy?.Replicas ?? 1,
                    RestartPolicy = new()
                    {
                        Condition = "on-failure",
                        Delay = "5s",
                        MaxAttempts = 5
                    }
                };
            } );
    }

    public static IResourceBuilder<ContainerResource> WithDefaultBuildArgs( this IResourceBuilder<ContainerResource> builder, IHostEnvironment environment )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( environment );

        return builder.WithBuildArg( "ENVIRONMENT", environment.EnvironmentName )
            .WithBuildArg( "VERSION", WadioVersion.Current.ToString() );
    }

    public static IResourceBuilder<T> WithPlatformDefaults<T>( this IResourceBuilder<T> builder, AppHostParameters parameters )
        where T : IComputeResource, IResourceWithEndpoints, IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( parameters );

        builder.WithHttpHealthCheck( HealthCheckDefaults.HealthEndpoint )
            .WithEnvironment( "Platform__PublicUrl", parameters.PublicUrl );

#pragma warning disable ASPIREPIPELINES003
        builder.WithRemoteImageName( $"wadio-{builder.Resource.Name}" )
            .WithRemoteImageTag( $"v{WadioVersion.Current.ToString( false )}" );
#pragma warning restore ASPIREPIPELINES003

        return builder;
    }
}