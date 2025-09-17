using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wadio.Extensions.RadioBrowser.Json;

public sealed class IPAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
        => IPAddress.TryParse( reader.GetString(), out var address ) ? address : default;

    public override void Write( Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options )
        => writer.WriteStringValue( value.ToString() );
}