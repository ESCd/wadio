using Microsoft.AspNetCore.Components.Forms;

namespace Wadio.App.UI.Components.Forms;

public sealed class EditContext<T>( T value )
    where T : notnull
{
    private readonly EditContext context = new( value );

    public T Value => value;

    public static implicit operator EditContext( EditContext<T> context ) => context.context;
}