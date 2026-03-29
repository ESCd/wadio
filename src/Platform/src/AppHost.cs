using Wadio.Platform;

var builder = DistributedApplication.CreateBuilder( args );

var parameters = AppHostParameters.Create( builder );
var compose = builder.AddDockerComposeEnvironment( "wadio" )
    .ConfigureComposeFile( compose =>
    {
        if( compose.Networks.Remove( "aspire", out var network ) )
        {
            network.Attachable = true;
            network.Driver = "overlay";
            compose.Networks.Add( "wadio", network );
        }

        foreach( var (_, service) in compose.Services )
        {
            if( service.Networks.Remove( "aspire" ) )
            {
                service.Networks.Add( "wadio" );
            }
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
                    Constraints = [ "node.labels.pool == manager" ]
                },
                Replicas = service.Deploy?.Replicas ?? 1,
                Resources = new()
                {
                    Limits = new()
                    {
                        Cpus = "0.5",
                        Memory = "0.5g"
                    }
                },
            };
        } ) );

var backplane = builder.AddGarnet( "backplane" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == platform" ]
            },
            Replicas = service.Deploy?.Replicas ?? 1,
            Resources = new()
            {
                Limits = new() { Cpus = "0.5" }
            },
            RestartPolicy = new() { Condition = "on-failure" }
        };
    } )
    .WithComputeEnvironment( compose );

var api = builder.AddProject<Projects.Api>( "api" )
    .PublishAsDockerComposeService( ( _, service ) =>
    {
        ArgumentNullException.ThrowIfNull( service );

        service.Deploy = new()
        {
            Placement = new()
            {
                Constraints = [ "node.labels.pool == platform" ]
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
        container => container.WithDockerfile( "../../..", "src/Platform/Api/src/Dockerfile" )
            .WithDefaultBuildArgs( builder.Environment ) )
    .WithComputeEnvironment( compose )
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
                Constraints = [ "node.labels.pool == platform" ]
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
        container => container.WithDockerfile( "../../..", "src/Platform/Discord/Dockerfile" )
            .WithDefaultBuildArgs( builder.Environment ) )
    .WithComputeEnvironment( compose )
    .WithPlatformDefaults( parameters )
    .WithReference( api )
    .WaitFor( api );

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
        container => container.WithDockerfile( "../../..", "src/Platform/Web/src/Dockerfile" )
            .WithDefaultBuildArgs( builder.Environment ) )
    .WithComputeEnvironment( compose )
    .WithPlatformDefaults( parameters )
    .WithReference( api )
    .WaitFor( api );

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
        container => container.WithDockerfile( "../../..", "src/Platform/Router/Dockerfile" )
            .WithDefaultBuildArgs( builder.Environment ) )
    .WithComputeEnvironment( compose )
    .WithPlatformDefaults( parameters )
    .WithExternalHttpEndpoints()
    .WithReference( api )
    .WaitFor( api )
    .WithReference( web )
    .WaitFor( web );

if( builder.ExecutionContext.IsPublishMode )
{
    discord.WithEnvironment( "Discord__Token", parameters.DiscordToken! );

    builder.AddContainer( "cloudflared", "cloudflare/cloudflared", "latest" )
        .WithArgs( "tunnel", "--no-autoupdate", "run" )
        .WithComputeEnvironment( compose )
        .WithEnvironment( "TUNNEL_TOKEN", parameters.CloudflareTunnelToken! )
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

#pragma warning disable ASPIRECOMPUTE003
    var registry = builder.AddContainerRegistry(
        "ghcr",
        "ghcr.io",
        "escd/wadio" );

    api.WithContainerRegistry( registry );
    discord.WithContainerRegistry( registry );
    router.WithContainerRegistry( registry );
    web.WithContainerRegistry( registry );
#pragma warning restore ASPIRECOMPUTE003
}

await using var app = builder.Build();
await app.RunAsync();