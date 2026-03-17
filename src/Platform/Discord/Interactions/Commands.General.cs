using NetCord.Services.ApplicationCommands;
using Wadio.Platform.Abstractions;

namespace Wadio.Platform.Discord.Interactions;

internal sealed partial class WadioCommands
{
    [SubSlashCommand( "ping", "Hello..? HELLO??" )]
    public static string Ping( ) => "I'm here. :YEP:";

    [SubSlashCommand( "version", "Get the current version of Wadio." )]
    public static string Version( ) => $"`v{WadioVersion.Current}`";
}