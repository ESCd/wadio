using Microsoft.Extensions.Hosting;
using Wadio.Platform.Abstractions;

namespace Wadio.Platform;

internal static class ResourceExtensions
{
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

        builder.WithHttpHealthCheck( "/health" )
            .WithEnvironment( "Platform__PublicUrl", parameters.PublicUrl );

#pragma warning disable ASPIREPIPELINES003
        builder.WithRemoteImageName( $"wadio-{builder.Resource.Name}" )
            .WithRemoteImageTag( $"v{WadioVersion.Current.ToString( false )}" );
#pragma warning restore ASPIREPIPELINES003

        return builder;
    }
}