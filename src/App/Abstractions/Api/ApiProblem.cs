namespace Wadio.App.Abstractions.Api;

/// <summary> Represents an error returned by the Wadio API. </summary>
public sealed record class ApiProblem
{
    /// <summary> A collection of errors describing the problem. </summary>
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

    /// <summary> The type of problem that occurred. </summary>
    public string? Type { get; init; }

    /// <summary> A user-friendly title of the problem. </summary>
    public string? Title { get; init; }

    /// <summary> A brief summary describing the problem. </summary>
    public string? Detail { get; init; }
}

/// <summary> Represents an <see cref="Exception"/> that occurs when making a request to the Wadio API. </summary>
public sealed class ApiProblemException( HttpRequestException inner, ApiProblem problem ) : HttpRequestException( inner.Message, inner, inner.StatusCode )
{
    /// <summary> The problem returned by the API. </summary>
    public ApiProblem Problem { get; } = problem;
}