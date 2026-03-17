using System.Diagnostics.CodeAnalysis;
using NetCord;
using NetCord.Services;

namespace Wadio.Platform.Discord.Infrastructure;

internal static class VoiceContextExtensions
{
    public static VoiceGuildChannel? GetUserVoiceChannel<TContext>( this TContext context, ulong userId )
        where TContext : class, IGuildContext
    {
        ArgumentNullException.ThrowIfNull( context );

        if( TryGetUserVoiceChannel( context, userId, out var channel ) )
        {
            return channel;
        }

        return default;
    }

    public static bool TryGetUserVoiceChannel<TContext>( this TContext context, ulong userId, [NotNullWhen( true )] out VoiceGuildChannel? channel )
        where TContext : class, IGuildContext
    {
        ArgumentNullException.ThrowIfNull( context );

        if( context.Guild?.VoiceStates.TryGetValue( userId, out var state ) is true && state.ChannelId.HasValue )
        {
            if( context.Guild.Channels.TryGetValue( state.ChannelId.Value, out var value ) )
            {
                channel = value as VoiceGuildChannel;
                return channel is not null;
            }
        }

        channel = default;
        return false;
    }
}