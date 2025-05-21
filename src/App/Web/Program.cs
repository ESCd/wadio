using Scalar.AspNetCore;
using Wadio.App.Web;
using Wadio.App.Web.Infrastructure;

var builder = WebApplication.CreateBuilder( args );
builder.Services.AddWadioWeb();

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

app.MapWadioApi();

app.MapOpenApi();
app.MapScalarApiReference( "/api" );

app.MapHealthChecks( "/healthz" )
    .AllowAnonymous()
    .WithRequestTimeout( TimeSpan.FromMinutes( 2 ) );

app.MapFallbackToController( "Index", "App" );
await app.RunAsync();
