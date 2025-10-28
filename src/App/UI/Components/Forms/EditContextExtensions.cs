using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Wadio.App.UI.Components.Forms;

internal static class EditContextExtensions
{
    public static IDisposable AddFieldChangedListener( this EditContext context, Action<EditContext, FieldChangedEventArgs> handler )
    {
        ArgumentNullException.ThrowIfNull( context );
        ArgumentNullException.ThrowIfNull( handler );

        context.OnFieldChanged += Handler;
        return new OnFieldChangedListener( context, Handler );

        void Handler( object? sender, FieldChangedEventArgs e )
        {
            var context = Unsafe.As<EditContext>( sender )!;
            handler( context, e );
        }
    }
}

sealed file class OnFieldChangedListener( EditContext context, EventHandler<FieldChangedEventArgs> handler ) : IDisposable
{
    public void Dispose( ) => context.OnFieldChanged -= handler;
}