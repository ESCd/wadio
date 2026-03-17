using NetCord;
using NetCord.Rest;

namespace Wadio.Platform.Discord.Infrastructure;

internal static class EmbedBuilderExtensions
{
    public static EmbedProperties WithUserColor( this EmbedProperties embed, Interaction interaction )
    {
        ArgumentNullException.ThrowIfNull( embed );
        ArgumentNullException.ThrowIfNull( interaction );

        return embed.WithColor( interaction.User.AccentColor ?? default );
    }
}