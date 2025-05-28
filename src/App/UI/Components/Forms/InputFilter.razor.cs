using System.Diagnostics.CodeAnalysis;

namespace Wadio.App.UI.Components.Forms;

public partial class InputFilter<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] TValue>
    where TValue : notnull;

public sealed record FilterOption( string Label, object Value )
{
    public long? Count { get; init; }
}

public interface IInputFilterProvider<T>
{
    public bool OnFiltering( FilterOption facet, string query, T value );
}

public static class StringFilterProvider
{
    public static readonly IInputFilterProvider<string> OrdinalIgnoreCase = new DelegateFilterProvider<string>( ( option, query, value )
        => option.Label.Contains( query, StringComparison.OrdinalIgnoreCase ) || value?.Contains( query, StringComparison.OrdinalIgnoreCase ) is true );
}

internal sealed class DelegateFilterProvider<T>( Func<FilterOption, string, T, bool> onFiltering ) : IInputFilterProvider<T>
{
    public bool OnFiltering( FilterOption option, string query, T value ) => onFiltering( option, query, value );
}