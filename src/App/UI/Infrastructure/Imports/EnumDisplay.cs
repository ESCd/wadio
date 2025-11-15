using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Wadio.App.UI.Infrastructure.Imports;

internal static class EnumDisplay
{
    private const DynamicallyAccessedMemberTypes DAM = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicNestedTypes;

    public static string GetName<[DynamicallyAccessedMembers( DAM )] T>( this T value )
        where T : struct, Enum
    {
        var type = typeof( T );
        var name = value.ToString();
        return type.GetMember( name ).Single().GetCustomAttribute<DisplayAttribute>()?.Name ?? name;
    }
}