using Wadio.Platform;

var builder = DistributedApplication.CreateBuilder( args );
var platform = builder.AddDockerComposeEnvironment( "platform" )
    .ConfigureComposeFile( compose =>
    {
        if( compose.Networks.Remove( "aspire", out var network ) )
        {
            network.Driver = "overlay";
            compose.Networks.Add( "platform", network );
        }

        foreach( var (_, service) in compose.Services )
        {
            service.Networks.Remove( "aspire" );
            service.Networks.Add( "platform" );
        }
    } )
    .WithDashboard( dashboard => dashboard.WithForwardedHeaders( true )
        .PublishAsDockerComposeService( ( _, service ) =>
        {
            ArgumentNullException.ThrowIfNull( service );

            service.Deploy = new()
            {
                Placement = new()
                {
                    Constraints = [ "node.labels.pool == platform" ]
                },
                Replicas = service.Deploy?.Replicas ?? 1
            };
        } ) );

var parameters = AppHostParameters.Create( builder );

var backplane = builder.AddGarnet( "backplane" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == api" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            RestartPolicy = new() { Condition = "on-failure" }
        };
    } )
    .WithComputeEnvironment( platform );

var api = builder.AddProject<Projects.Api>( "api" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == api" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            RestartPolicy = new() { Condition = "on-failure" },
            UpdateConfig = new()
            {
                Parallelism = "1",
                Delay = "10s",
                Order = "start-first"
            },
        };
    } )
    .PublishAsDockerFile(
        container => container.WithDefaultBuildArgs( builder.Environment )
            .WithDockerfile( "../../..", "src/Platform/Api/src/Dockerfile" ) )
    .WithComputeEnvironment( platform )
    .WithPlatformDefaults( parameters )
    .WithReference( backplane )
    .WaitFor( backplane );

var discord = builder.AddProject<Projects.Discord>( "discord" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == discord" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            RestartPolicy = new()
            {
                Condition = "on-failure",
                Delay = "10s",
                MaxAttempts = 5
            }
        };

        service.Restart = "unless-stopped";
    } )
    .PublishAsDockerFile(
        container => container.WithDefaultBuildArgs( builder.Environment )
            .WithDockerfile( "../../..", "src/Platform/Discord/Dockerfile" ) )
    .WithComputeEnvironment( platform )
    .WithPlatformDefaults( parameters );

var web = builder.AddProject<Projects.Web>( "web" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == web" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            RestartPolicy = new() { Condition = "on-failure" },
            UpdateConfig = new()
            {
                Parallelism = "1",
                Delay = "10s",
                Order = "start-first"
            },
        };
    } )
    .PublishAsDockerFile(
        container => container.WithDefaultBuildArgs( builder.Environment )
            .WithDockerfile( "../../..", "src/Platform/Web/src/Dockerfile" ) )
    .WithComputeEnvironment( platform )
    .WithPlatformDefaults( parameters );

var router = builder.AddProject<Projects.Router>( "router" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == web" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            RestartPolicy = new() { Condition = "on-failure" },
        };
    } )
    .PublishAsDockerFile(
        container => container.WithDefaultBuildArgs( builder.Environment )
            .WithDockerfile( "../../..", "src/Platform/Router/Dockerfile" ) )
    .WithComputeEnvironment( platform )
    .WithPlatformDefaults( parameters )
    .WithExternalHttpEndpoints()
    .WithReference( api )
    .WithReference( web )
    .WaitFor( api )
    .WaitFor( web );

if( builder.ExecutionContext.IsPublishMode )
{
    discord.WithEnvironment( "Discord__Token", parameters.DiscordToken! );
    WithPublicApi( discord, parameters.PublicUrl );
    WithPublicApi( web, parameters.PublicUrl );

    builder.AddContainer( "cloudflared", "cloudflare/cloudflared", "latest" )
        .WithArgs( "tunnel", "--no-autoupdate", "run" )
        .WithComputeEnvironment( platform )
        .WithEnvironment( "TUNNEL_TOKEN", parameters.CloudflareTunnelToken! )
        .PublishAsDockerComposeService( ( _, service ) =>
        {
            ArgumentNullException.ThrowIfNull( service );

            service.Deploy = new()
            {
                Placement = new()
                {
                    Constraints = [ "node.labels.pool == web" ]
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

#pragma warning disable ASPIRECOMPUTE003
    var registry = builder.AddContainerRegistry(
        "ghcr",
        "ghcr.io",
        "ESCd/wadio" );

    api.WithContainerRegistry( registry );
    discord.WithContainerRegistry( registry );
    router.WithContainerRegistry( registry );
    web.WithContainerRegistry( registry );
#pragma warning restore ASPIRECOMPUTE003

    static void WithPublicApi( IResourceBuilder<ProjectResource> builder, IResourceBuilder<ParameterResource> url )
    {
        ArgumentNullException.ThrowIfNull( builder );
        ArgumentNullException.ThrowIfNull( url );

        builder.WithEnvironment( "Services__api__http__0", $"{url}/api" )
            .WithEnvironment( "Services__api__https__0", $"{url}/api" );
    }
}
else
{
    discord.WithReference( api ).WaitFor( api );
    web.WithReference( api ).WaitFor( api );
}

await using var app = builder.Build();
await app.RunAsync();