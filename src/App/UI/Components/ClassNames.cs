using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using Wadio.App.UI.Infrastructure;

namespace Wadio.App.UI.Components;

internal static class ClassNames
{
    private static readonly ObjectPool<HashSet<string>> HashSetPool = ObjectPool.Create( new HashSetPooledPolicy<string>()
    {
        Comparer = StringComparer.Ordinal,
        InitialCapacity = 64,
        MaximumRetainedCapacity = 512,
    } );

    public static string Combine( params ImmutableArray<string?> values )
    {
        return string.Join( ' ', Normalize( values ) );

        [MethodImpl( MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization )]
        static IEnumerable<string> Normalize( ImmutableArray<string?> values )
        {
            if( values.Length is 0 )
            {
                yield break;
            }

            var seen = HashSetPool.Get();
            try
            {
                foreach( var value in values )
                {
                    var chunks = value?.Split( ' ', StringSplitOptions.RemoveEmptyEntries );
                    if( chunks?.Length is null or 0 )
                    {
                        continue;
                    }

                    foreach( var chunk in chunks )
                    {
                        var normalized = chunk.Trim();
                        if( normalized.Length is not 0 && seen.Add( normalized ) )
                        {
                            yield return normalized;
                        }
                    }
                }
            }
            finally
            {
                HashSetPool.Return( seen );
            }
        }
    }

    public static string Combine( IReadOnlyDictionary<string, object>? attributes, params ImmutableArray<string?> values )
    {
        if( attributes?.TryGetValue( "class", out var value ) is true && value is string @class )
        {
            return Combine( [ .. values, @class ] );
        }

        return Combine( values );
    }
}