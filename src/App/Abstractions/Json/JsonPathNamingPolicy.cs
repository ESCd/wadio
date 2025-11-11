using System.Text.Json;

namespace Wadio.App.Abstractions.Json;

public sealed class JsonPathNamingPolicy( JsonNamingPolicy policy ) : JsonNamingPolicy
{
    public static new readonly JsonPathNamingPolicy CamelCase = new( JsonNamingPolicy.CamelCase );

    public override string ConvertName( string name ) => string.Join(
        '.',
        name.Split( '.' ).Select( policy.ConvertName ) );
}