using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Components;

internal sealed class ErrorDetails
{
    public ApiProblem? ApiProblem { get; private init; }
    public string? Message { get; private init; }
    public string? Type { get; private init; }
    public string? Source { get; private init; }
    public string? StackTrace { get; private init; }
    public WadioVersion Version { get; } = WadioVersion.Current;
    public Uri Url { get; private init; }

    public static ErrorDetails Create( NavigationManager navigation, Exception exception )
    {
        ArgumentNullException.ThrowIfNull( navigation );
        ArgumentNullException.ThrowIfNull( exception );

        return new()
        {
            ApiProblem = (exception as ApiProblemException)?.Problem,
            Message = $"{exception}",
            Source = exception.Source,
            StackTrace = exception.StackTrace,
            Type = exception.GetType().FullName,
            Url = new( navigation.Uri ),
        };
    }

    public string ToJsonString( ) => JsonSerializer.Serialize( this, ErrorJsonContext.Default.ErrorDetails );
}

[JsonSerializable( typeof( ErrorDetails ) )]
[JsonSourceGenerationOptions( JsonSerializerDefaults.General, WriteIndented = true )]
internal sealed partial class ErrorJsonContext : JsonSerializerContext;