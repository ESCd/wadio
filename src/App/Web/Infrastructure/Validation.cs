using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Wadio.App.Web.Infrastructure;

internal static class Validation
{
    public static void ThrowIfInvalid<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( T value )
    {
        ArgumentNullException.ThrowIfNull( value );
        Validator.ValidateObject( value, new( value ), true );
    }

    public static bool TryValidate<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.All )] T>( T model, [NotNullWhen( false )] out Dictionary<string, string[]>? errors )
    {
        ArgumentNullException.ThrowIfNull( model );

        var results = new List<ValidationResult>();
        if( !Validator.TryValidateObject( model, new( model ), results, true ) )
        {
            errors = [];
            foreach( var result in results )
            {
                foreach( var member in result.MemberNames )
                {
                    if( !errors.TryGetValue( member, out var value ) )
                    {
                        errors.Add( member, [ result.ErrorMessage ?? "" ] );
                        continue;
                    }

                    errors[ member ] = [ .. value, result.ErrorMessage ?? "" ];
                }
            }

            return false;
        }

        errors = default;
        return true;
    }
}