namespace Wadio.Platform.Api.Abstractions;

public abstract record Signal<T>( ) where T : Signal<T>;

public static class MetadataSignal
{
    public sealed record class Metadata( IReadOnlyDictionary<string, string> Data ) : Signal<Metadata>;
}