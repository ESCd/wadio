using System.Text.Json;
using System.Text.Json.Serialization;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.UI.Components;

internal sealed class ErrorDetails
{
    public ApiProblem? ApiProblem { get; private init; }
    public string? Message { get; private init; }
    public string? Type { get; private init; }
    public string? Source { get; private init; }
    public string? StackTrace { get; private init; }
    public AppVersion Version { get; } = AppVersion.Value;

    public static ErrorDetails Create( Exception exception )
    {
        ArgumentNullException.ThrowIfNull( exception );
        return new()
        {
            ApiProblem = (exception as ApiProblemException)?.Problem,
            Message = $"{exception}",
            Source = exception.Source,
            StackTrace = exception.StackTrace,
            Type = exception.GetType().FullName
        };
    }

    public string ToJsonString( ) => JsonSerializer.Serialize( this, ErrorJsonContext.Default.ErrorDetails );
}

[JsonSerializable( typeof( ErrorDetails ) )]
[JsonSourceGenerationOptions( JsonSerializerDefaults.General, WriteIndented = true )]
internal sealed partial class ErrorJsonContext : JsonSerializerContext;