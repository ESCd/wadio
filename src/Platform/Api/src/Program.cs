using Scalar.AspNetCore;
using Wadio.Platform.Api;
using Wadio.Platform.Api.Infrastructure;
using Wadio.Platform.Hosting;

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
app.UseRouting();
app.UseRequestTimeouts();
app.UseRequestCancellation();

app.MapWadioApi( "/" );

app.MapOpenApi();
app.MapPlatformEndpoints();
app.MapScalarApiReference( "/" );

await app.RunAsync();