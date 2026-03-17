using System.ComponentModel.DataAnnotations;

namespace Wadio.Platform.Discord.Abstractions;

public sealed class WadioBotOptions
{
    [Required]
    public Uri WebEndpoint { get; set; }
}