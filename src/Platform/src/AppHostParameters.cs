namespace Wadio.Platform;

internal sealed record AppHostParameters(
    IResourceBuilder<ParameterResource> CloudflareImagesAccount,
    IResourceBuilder<ParameterResource> CloudflareImagesHash,
    IResourceBuilder<ParameterResource> CloudflareImagesToken,
    IResourceBuilder<ParameterResource>? CloudflareTunnelToken,
    IResourceBuilder<ParameterResource> DiscordToken,
    IResourceBuilder<ParameterResource> PublicUrl )
{
    public static AppHostParameters Create( IDistributedApplicationBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        return new(
            builder.AddParameter( "cf-images-account", secret: true ),
            builder.AddParameter( "cf-images-hash", secret: true ),
            builder.AddParameter( "cf-images-token", secret: true ),
            builder.ExecutionContext.IsPublishMode ? builder.AddParameter( "cf-tunnel-token", secret: true ) : default,
            builder.AddParameter( "discord-token", secret: true ),
            builder.AddParameter( "public-url" ) );
    }
}