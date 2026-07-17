using Wadio.Platform.Hosting;
using Wadio.Platform.Web;
using Wadio.Platform.Web.Infrastructure;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithWadioWeb();

await using var app = builder.Build();
if( app.Environment.IsDevelopment() )
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCookiePolicy();

app.UseRequestDecompression();
app.UseResponseCaching();
if( !app.Environment.IsDevelopment() )
{
    app.UseResponseCompression();
}

app.UseCors();
app.UseOutputCache();
app.UseRequestTimeouts();
app.UseRequestCancellation();

app.UseWebSockets();
app.UseRouting();

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();

app.MapOpenSearch();
app.MapPlatformEndpoints();

app.MapFallbackToController( "Index", "App" );
await app.RunAsync();