using Wadio.Platform.Discord;
using Wadio.Platform.Hosting;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithWadioBot();

await using var app = builder.Build();

app.UseHttpsRedirection();

app.UseWadioBot();
app.MapPlatformEndpoints();

await app.RunAsync();
