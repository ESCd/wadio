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
    .AddStandardResilienceHandler( options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds( 30 );
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes( 2.5 );

        options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
    } );

await using var app = builder.Build();

Console.WriteLine( $"Wadio v{WadioVersion.Current} ({builder.HostEnvironment.Environment})" );
await app.RunAsync();