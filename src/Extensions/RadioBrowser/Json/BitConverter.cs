using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Json;

public sealed class BitConverter : JsonConverter<bool>
{
    public override bool Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        var value = reader.GetByte();
        return value is 1;
    }

    public override void Write( Utf8JsonWriter writer, bool value, JsonSerializerOptions options )
    {
        writer.WriteNumberValue( value ? 1 : 0 );
    }
}