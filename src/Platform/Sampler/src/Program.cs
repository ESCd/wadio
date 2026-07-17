using Scalar.AspNetCore;
using Wadio.Platform.Hosting;
using Wadio.Platform.Hosting.Infrastructure;
using Wadio.Platform.Sampler;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults()
    .WithSamplerApi();

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

app.MapSamplerApi( "/" );

app.MapOpenApi();
app.MapScalarApiReference( "/" );

app.MapPlatformEndpoints();
await app.RunAsync();