namespace Wadio.Platform;

internal sealed record AppHostParameters(
    IResourceBuilder<ParameterResource>? CloudflareTunnelToken,
    IResourceBuilder<ParameterResource>? DiscordToken,
    IResourceBuilder<ParameterResource> PublicUrl )
{
    public static AppHostParameters Create( IDistributedApplicationBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        return new(
            builder.ExecutionContext.IsPublishMode ? builder.AddParameter( "cf-tunnel-token", secret: true ) : default,
            // builder.ExecutionContext.IsPublishMode ? builder.AddParameter( "discord-token", secret: true ) : default,
            builder.AddParameter( "discord-token", secret: true ),
            builder.AddParameter( "public-url" ) );
    }
}