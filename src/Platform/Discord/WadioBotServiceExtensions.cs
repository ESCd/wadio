using System.Threading.Channels;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Rest;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Wadio.Extensions.Icecast;
using Wadio.Platform.Api.Client;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Configuration;
using Wadio.Platform.Discord.Infrastructure;
using Wadio.Platform.Discord.Infrastructure.Playback;
using Wadio.Platform.Sampler.Client;
using Channel = System.Threading.Channels.Channel;

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
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>()
            .AddTransient<IComponentContextFactory, ComponentContextFactory>()
            .AddIcecastClient()
            .AddMetadataSampler()
            .AddWadioApiClient( api => api.ConfigureHttpClient( http => http.BaseAddress = new( "https+http://api/" ) ) );

        builder.Services.AddSingleton<StationPlayerContext>()
            .AddHostedService( services => services.GetRequiredService<StationPlayerContext>() )
            .AddSingleton( Channel.CreateBounded<StationPlayerAction>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            } ) );

        builder.Services.AddSingleton<StationPlayerFactory>()
            .AddHostedService( services => services.GetRequiredService<StationPlayerFactory>() )
            .AddSingleton( Channel.CreateBounded<StationPlayerFactory.CreateAction>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            } ) );

        builder.Services.AddSingleton<StationPlayerRenderer>()
            .AddHostedService( services => services.GetRequiredService<StationPlayerRenderer>() )
            .AddSingleton( Channel.CreateBounded<StationPlayerRenderRequest>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            } ) );

        builder.Services.ConfigureOptions<ConfigureApplicationCommands>();

        return builder;
    }
}