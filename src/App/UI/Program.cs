using ESCd.Extensions.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI;
using Wadio.App.UI.Infrastructure;

var builder = WebAssemblyHostBuilder.CreateDefault( args );
builder.Logging.SetMinimumLevel( builder.HostEnvironment.IsDevelopment()
    ? LogLevel.Information
    : LogLevel.Error );

builder.Services.AddWadioUI();

builder.Services.AddTransient<ApiProblemHandler>()
    .AddQueryStringBuilderObjectPool()
    .AddHttpClient<IWadioApi, WadioApi>( http => http.BaseAddress = new( builder.HostEnvironment.BaseAddress + "api/" ) )
    .AddHttpMessageHandler<ApiProblemHandler>()
    .AddStandardResilienceHandler();

await using var app = builder.Build();

Console.WriteLine( $"Wadio v{AppVersion.Value} ({builder.HostEnvironment.Environment})" );
await app.RunAsync();