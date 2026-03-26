using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "search", "Search for Stations." )]
    public static InteractionCallbackProperties Search(
        [SlashCommandParameter( Description = "The name of a Station." )] string? name = default,
        [SlashCommandParameter( Description = "The codec of a Station." )] Codec? codec = default,
        [SlashCommandParameter] bool? location = default,
        [SlashCommandParameter] StationOrderBy? order = default,
        [SlashCommandParameter] bool? reverse = default )
        => InteractionCallback.Modal( SearchModal.Create( new()
        {
            Codec = codec,
            HasLocation = location,
            Name = name,
            Order = order,
            Reverse = reverse
        } ) );
}