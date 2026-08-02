using Wadio.Platform.Api.Client;
using Wadio.Platform.Hosting;
using Wadio.Platform.Router.Endpoints;
using Wadio.Platform.Router.Infrastructure;

var builder = WebApplication.CreateBuilder( args )
    .WithPlatformDefaults();

builder.Services.AddCors()
    .AddOutputCache()
    .AddRequestDecompression()
    .AddResponseCaching()
    .AddResponseCompression()
    .AddWadioApiClient( api => api.ConfigureHttpClient( http => http.BaseAddress = new( "https+http://api/" ) ) )
    .AddSingleton<StationIconForwarder>();

builder.Services.AddHttpForwarder()
    .AddReverseProxy()
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

app.MapGet( "/ico/station/{stationId:guid}", IconEndpoints.Station );

app.MapPlatformEndpoints();
app.MapReverseProxy();

await app.RunAsync();