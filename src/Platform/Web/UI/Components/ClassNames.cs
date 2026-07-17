using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Wadio.Platform.Web.UI.Components;

internal static class ClassNames
{
    public static string Combine( params FrozenSet<string?> values )
    {
        if( values.Count is 0 )
        {
            return "";
        }

        values = [ .. Normalize( values ) ];
        return string.Join( ' ', values );

        [MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
        static IEnumerable<string> Normalize( FrozenSet<string?> values )
        {
            if( values.Count is 0 )
            {
                yield break;
            }

            foreach( var value in values )
            {
                if( string.IsNullOrWhiteSpace( value ) )
                {
                    continue;
                }

                var chunks = value.Split( ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
                if( chunks.Length is 0 )
                {
                    continue;
                }

                foreach( var chunk in chunks )
                {
                    yield return chunk;
                }
            }
        }
    }

    public static string Combine( IReadOnlyDictionary<string, object>? attributes, params FrozenSet<string?> values )
    {
        if( attributes?.TryGetValue( "class", out var value ) is true && value is string @class )
        {
            return Combine( [ .. values, @class ] );
        }

        return Combine( values );
    }
}