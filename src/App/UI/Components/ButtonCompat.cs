using Microsoft.AspNetCore.Components.Web;

namespace Wadio.App.UI.Components;

internal static class ButtonCompat
{
    public static Func<KeyboardEventArgs, Task> OnKeyDown( Func<Task> handler )
    {
        ArgumentNullException.ThrowIfNull( handler );

        return e =>
        {
            if( e.Code is "Enter" or "Space" )
            {
                return handler();
            }

            return Task.CompletedTask;
        };
    }
}