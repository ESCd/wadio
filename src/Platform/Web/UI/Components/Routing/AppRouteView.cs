using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Wadio.Platform.Web.UI.Components.Layout;

namespace Wadio.Platform.Web.UI.Components.Routing;

public sealed class AppRouteView : RouteView
{
    public AppRouteView( )
    {
        DefaultLayout = typeof( AppLayout );
    }

    /// <inheritdoc />
    protected override void Render( RenderTreeBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.OpenComponent<ErrorDialog>( 0 );
        builder.AddComponentParameter( 1, nameof( ErrorDialog.ChildContent ), new RenderFragment( base.Render ) );
        builder.CloseComponent();
    }
}