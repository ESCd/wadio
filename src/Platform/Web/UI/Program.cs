using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wadio.Platform.Abstractions;
using Wadio.Platform.Api.Client;
using Wadio.Platform.Web.UI;

var builder = WebAssemblyHostBuilder.CreateDefault( args );
builder.Logging.SetMinimumLevel( builder.HostEnvironment.IsDevelopment()
    ? LogLevel.Information
    : LogLevel.Error );

builder.Services.AddWadioUI()
    .AddWadioApiClient( api => api.ConfigureHttpClient( http => http.BaseAddress = new( builder.HostEnvironment.BaseAddress + "api/" ) ) );

await using var app = builder.Build();

Console.WriteLine( $"Wadio v{WadioVersion.Current} ({builder.HostEnvironment.Environment})" );
await app.RunAsync();