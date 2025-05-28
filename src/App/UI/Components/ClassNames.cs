namespace Wadio.App.UI.Components;

internal static class ClassNames
{
    public static string Combine( params string?[] classNames ) => string.Join(
        " ",
        classNames.Select( className => className?.Trim() ).Where( className => !string.IsNullOrWhiteSpace( className ) ) );

    public static string Combine( IReadOnlyDictionary<string, object>? attributes, params string?[] classNames )
    {
        if( attributes?.TryGetValue( "class", out var value ) is true && value is string @class )
        {
            return Combine( [ .. classNames, @class ] );
        }

        return Combine( classNames );
    }
}
