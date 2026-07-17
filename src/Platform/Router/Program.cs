using Wadio.Platform.Hosting;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults();

builder.Services.AddCors()
    .AddOutputCache()
    .AddRequestDecompression()
    .AddResponseCaching()
    .AddResponseCompression();

builder.Services.AddReverseProxy()
    .LoadFromConfig( builder.Configuration.GetSection( "ReverseProxy" ) )
    .AddServiceDiscoveryDestinationResolver();

await using var app = builder.Build();
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
app.UseCookiePolicy();

app.UseRequestDecompression();
app.UseResponseCaching();
if( !app.Environment.IsDevelopment() )
{
    app.UseResponseCompression();
}

app.UseCors();
app.UseOutputCache();
app.UseWebSockets();

app.MapPlatformEndpoints();
app.MapReverseProxy();

await app.RunAsync();