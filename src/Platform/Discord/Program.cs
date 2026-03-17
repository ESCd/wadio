using NetCord.Hosting.Services.ApplicationCommands;
using Wadio.Platform.Discord;
using Wadio.Platform.Discord.Interactions;
using Wadio.Platform.Hosting;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithWadioBot();

await using var app = builder.Build();

app.UseHttpsRedirection();
app.MapPlatformEndpoints();

app.AddApplicationCommandModule<WadioCommands>();

await app.RunAsync();
