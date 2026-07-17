using System.Security.Cryptography;
using System.Text;
using NetCord;

namespace Wadio.Platform.Discord.Infrastructure;

internal static class WadioColor
{
    public static readonly Color Default = Convert( "WADIO" );

    public static Color Convert( Guid? value )
    {
        if( !value.HasValue || value == Guid.Empty )
        {
            return Default;
        }

        ReadOnlySpan<byte> hash = value.Value.ToByteArray();

        var (r, g, b) = (
            hash[ 0 ],
            hash[ hash.Length / 2 ],
            hash[ ^1 ]);

        return new( r, g, b );
    }

    public static Color Convert( string? value )
    {
        if( string.IsNullOrWhiteSpace( value ) )
        {
            return Default;
        }

        ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes( value );
        ReadOnlySpan<byte> hash = SHA256.HashData( data );

        var (r, g, b) = (
            hash[ 0 ],
            hash[ hash.Length / 2 ],
            hash[ ^1 ]);

        return new( r, g, b );
    }
}