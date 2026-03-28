using Wadio.Platform.Hosting;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig( builder.Configuration.GetSection( "ReverseProxy" ) )
    .AddServiceDiscoveryDestinationResolver();

await using var app = builder.Build();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.MapPlatformEndpoints();
app.MapReverseProxy();

await app.RunAsync();