namespace Wadio.App.UI.Components;

public enum ImageLoadStrategy : byte
{
    Auto = 0,
    Eager = 1,
    Lazy = 2
}

public static class ImageLoadStrategyExtensions
{
    public static string? ToAttributeValue( this ImageLoadStrategy loading ) => loading switch
    {
        ImageLoadStrategy.Eager => "eager",
        ImageLoadStrategy.Lazy => "lazy",

        _ => default
    };
}