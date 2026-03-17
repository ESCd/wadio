using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder( args );

var api = builder.AddProject<Projects.Wadio_Platform_Api>( "api" )
    .WithHttpHealthCheck( "/health" );

if( builder.Environment.IsDevelopment() )
{
    api.WithExternalHttpEndpoints();
}

var web = builder.AddProject<Projects.Wadio_Platform_Web>( "web" )
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck( "/health" )
    .WithReference( api )
    .WaitFor( api );

builder.AddProject<Projects.Wadio_Platform_Discord>( "discord" )
    .WithHttpHealthCheck( "/health" )
    .WithReference( api )
    .WithReference( web )
    .WaitFor( api );

await using var app = builder.Build();
await app.RunAsync();