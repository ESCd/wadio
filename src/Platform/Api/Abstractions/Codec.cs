using System.ComponentModel.DataAnnotations;

namespace Wadio.Platform.Api.Abstractions;

public enum Codec : byte
{
    Unknown,
    AAC,

    [Display( Name = "AAC+" )]
    AACPlus,

    [Display( Name = "AAC, H.264" )]
    AACH264,

    [Display( Name = "AAC+, H.264" )]
    AACPlusH264,
    FLAC,
    FLV,
    MP3,
    OGG,
}

public static class CodecString
{
    public static string Format( Codec codec ) => codec switch
    {
        Codec.AACH264 => "aac,h.264",
        Codec.AACPlus => "aac+",
        Codec.AACPlusH264 => "aac+,h.264",
        _ => codec.ToString().ToLowerInvariant(),
    };

    public static Codec Parse( string? value )
    {
        if( Enum.TryParse<Codec>( value, out var codec ) )
        {
            return codec;
        }

        return value switch
        {
            "AAC,H.264" or "aac,h.264" => Codec.AACH264,
            "AAC+" or "aac+" => Codec.AACPlus,
            "AAC+,H.264" or "aac+,h.264" => Codec.AACPlusH264,
            _ => Codec.Unknown,
        };
    }
}