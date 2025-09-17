using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Wadio.Extensions.Icecast.Abstractions;

public sealed class IcecastMetadataDictionary : IReadOnlyDictionary<string, string>
{
    private readonly IDictionary<string, string> values;

    public int Interval { get; }
    public string? StreamTitle => GetMemberValue();
    public string? StreamGenre => GetMemberValue();
    public string? StreamUrl => GetMemberValue();

    public string this[ string key ] => values[ key ];
    public IEnumerable<string> Keys => values.Keys;
    public IEnumerable<string> Values => values.Values;
    public int Count => values.Count;

    internal IcecastMetadataDictionary( int interval, IDictionary<string, string> values )
    {
        ArgumentNullException.ThrowIfNull( values );

        Interval = interval;
        this.values = values;
        this.values.TryAdd( nameof( Interval ), interval.ToString( CultureInfo.InvariantCulture ) );
    }

    public bool ContainsKey( string key ) => values.ContainsKey( key );
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator( ) => values.GetEnumerator();

    private string? GetMemberValue( [CallerMemberName] string key = "" )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( key );
        if( TryGetValue( key, out var value ) )
        {
            return value;
        }

        return default;
    }

    public bool TryGetValue( string key, [MaybeNullWhen( false )] out string value ) => values.TryGetValue( key, out value );
    IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator();
}