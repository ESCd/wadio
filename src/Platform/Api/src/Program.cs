using Scalar.AspNetCore;
using Wadio.Platform.Api;
using Wadio.Platform.Api.Infrastructure;
using Wadio.Platform.Hosting;
using Wadio.Platform.Hosting.Infrastructure;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithWadioApi();

await using var app = builder.Build();
if( app.Environment.IsDevelopment() )
{
    app.UseDeveloperExceptionPage();
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

app.UseRequestDecompression();
app.UseResponseCaching();
if( !app.Environment.IsDevelopment() )
{
    app.UseResponseCompression();
}

app.UseCors();
app.UseRequestTimeouts();
app.UseRequestCancellation();

app.UseRouting();
app.MapStaticAssets();

app.MapWadioApi( "/" );

app.MapOpenApi();
app.MapScalarApiReference( "/" );

app.MapPlatformEndpoints();
await app.RunAsync();