using System.Threading.Channels;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Rest;
using NetCord.Hosting.Services.ApplicationCommands;
using Wadio.Extensions.Icecast;
using Wadio.Platform.Api.Client;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Infrastructure;

namespace Wadio.Platform.Discord;

public static class WadioBotServiceExtensions
{
    public static TBuilder WithWadioBot<TBuilder>( this TBuilder builder )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.Services.AddHostedService<WadioBot>()
            .AddDiscordGateway()
            .AddDiscordRest()
            .AddApplicationCommands()
            .AddIcecastClient()
            .AddWadioApiClient( api => api.ConfigureHttpClient( http => http.BaseAddress = new( "https+http://api/" ) ) );

        builder.Services.AddSingleton<StationPlayerFactory>()
            .AddHostedService( services => services.GetRequiredService<StationPlayerFactory>() )
            .AddSingleton( Channel.CreateBounded<StationPlayerFactory.Request>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            } ) );

        builder.Services.AddHostedService<StationPlayerWorker>()
            .AddSingleton( Channel.CreateBounded<StationPlayerRequest>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            } ) );

#pragma warning disable IL2026,IL3050
        builder.Services.AddOptions<WadioBotOptions>()
            .BindConfiguration( "WadioBot" )
            .ValidateDataAnnotations()
            .ValidateOnStart();
#pragma warning restore IL2026,IL3050

        return builder;
    }
}