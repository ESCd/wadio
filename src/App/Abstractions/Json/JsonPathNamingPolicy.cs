using System.Text.Json;

namespace Wadio.App.Abstractions.Json;

public sealed class JsonPathNamingPolicy( JsonNamingPolicy policy ) : JsonNamingPolicy
{
    public override string ConvertName( string name ) => string.Join(
        '.',
        name.Split( '.' ).Select( policy.ConvertName ) );
}