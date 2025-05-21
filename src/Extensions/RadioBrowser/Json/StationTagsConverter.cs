using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Json;

internal sealed class CommaDelimitedConverter : JsonConverter<string[]>
{
    public override string[]? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
        => reader.GetString()?.Split( ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries ) ?? [];

    public override void Write( Utf8JsonWriter writer, string[] value, JsonSerializerOptions options )
    {
        writer.WriteStartArray();
        foreach( var tag in value )
        {
            writer.WriteStringValue( tag );
        }

        writer.WriteEndArray();
    }
}