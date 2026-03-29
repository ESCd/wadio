using Wadio.Platform.Discord;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Hosting;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithWadioBot();

await using var app = builder.Build();

app.UseUnhandledExceptionLogging();
if( app.Environment.IsDevelopment() )
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseWadioBot();
app.MapPlatformEndpoints();

await app.RunAsync();
