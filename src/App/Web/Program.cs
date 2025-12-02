using Scalar.AspNetCore;
using Wadio.App.Web;
using Wadio.App.Web.Infrastructure;

var builder = WebApplication.CreateBuilder( args )
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

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseDeprecatedApiHeader();
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
app.UseRouting();
app.UseRequestTimeouts();
app.UseRequestCancellation();

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();
app.MapOpenSearch();

app.MapWadioApi();

app.MapOpenApi();
app.MapScalarApiReference( "/api" );

app.MapHealthChecks( "/healthz" )
    .AllowAnonymous()
    .DisableRequestTimeout();

app.MapHealthChecks( "/alivez", new()
{
    Predicate = r => r.Tags.Contains( "live" )
} ).AllowAnonymous().DisableRequestTimeout();

app.MapFallbackToController( "Index", "App" );
await app.RunAsync();