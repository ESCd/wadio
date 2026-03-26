namespace Wadio.Platform;

internal sealed record AppHostParameters(
    IResourceBuilder<ParameterResource>? CloudflareTunnelToken,
    IResourceBuilder<ParameterResource> PublicUrl )
{
    public static AppHostParameters Create( IDistributedApplicationBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        return new(
            builder.ExecutionContext.IsPublishMode ? builder.AddParameter( "cf-tunnel-token", secret: true ) : default,
            builder.AddParameter( "public-url" ) );
    }
}