using System.ComponentModel.DataAnnotations;

namespace Wadio.Platform.Hosting.Abstractions;

public sealed class PlatformOptions
{
    [Required]
    public Uri PublicUrl { get; set; }
}