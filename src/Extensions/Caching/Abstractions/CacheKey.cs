using System.Collections.Immutable;
using System.ComponentModel;

namespace Wadio.Extensions.Caching.Abstractions;

[ImmutableObject( true )]
public sealed class CacheKey( params ImmutableArray<string> parts ) : IEquatable<CacheKey>
{
    private const char Delimiter = '|';

    public ImmutableArray<string> Parts { get; } = parts;

    public CacheKey( CacheKey key, params ImmutableArray<string> parts ) : this( [ .. key.Parts, .. parts ] )
    {
    }

    public bool Equals( CacheKey? other ) => other?.Parts.SequenceEqual( Parts, StringComparer.OrdinalIgnoreCase ) is true;

    public override bool Equals( object? value ) => value is CacheKey key && Equals( key );

    public override int GetHashCode( )
    {
        var hash = new HashCode();
        foreach( var key in Parts )
        {
            hash.Add( key );
        }

        return hash.ToHashCode();
    }

    public override string ToString( ) => string.Join( Delimiter, Parts );

    public static bool operator ==( CacheKey left, CacheKey right ) => left.Equals( right );
    public static bool operator !=( CacheKey left, CacheKey right ) => !left.Equals( right );
}