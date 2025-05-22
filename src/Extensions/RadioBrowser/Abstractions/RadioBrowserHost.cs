using System.Net;
using System.Text.Json.Serialization;
using Wadio.Extensions.RadioBrowser.Json;

namespace Wadio.Extensions.RadioBrowser.Abstractions;

public sealed record RadioBrowserHost
{
    [JsonConverter( typeof( IPAddressConverter ) )]
    [JsonPropertyName( "ip" )]
    public IPAddress Address { get; init; }
    public string? Name { get; init; }
}

public static class RadioBrowserHostExtensions
{
    public static Uri BuildUrl( this RadioBrowserHost host, Uri url )
    {
        ArgumentNullException.ThrowIfNull( host );
        ArgumentNullException.ThrowIfNull( url );

        var builder = new UriBuilder( url )
        {
            Host = !string.IsNullOrEmpty( host.Name ) ? host.Name : host.Address.ToString(),
            Scheme = "https://",
        };

        return builder.Uri;
    }
}