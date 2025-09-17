namespace Wadio.App.Abstractions.Signals;

public static class MetadataSignals
{
    public sealed record Metadata( IReadOnlyDictionary<string, string> Data ) : Signal<Metadata>;
}