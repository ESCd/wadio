using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Json;

public sealed class UrlConverter : JsonConverter<Uri>
{
    public override Uri? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if( Uri.TryCreate(
            reader.GetString(),
            UriKind.Absolute,
            out var url ) )
        {
            return url;
        }

        return default;
    }

    public override void Write( Utf8JsonWriter writer, Uri value, JsonSerializerOptions options )
        => writer.WriteStringValue( value.AbsoluteUri );
}