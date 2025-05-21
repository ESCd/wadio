using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Wadio.App.UI.Components.Routing;

public sealed class AppRouteView : RouteView
{
    /// <inheritdoc />
    protected override void Render( RenderTreeBuilder builder )
    {
        ArgumentNullException.ThrowIfNull( builder );

        builder.OpenComponent<ErrorDialog>( 0 );
        builder.AddComponentParameter( 1, nameof( ErrorDialog.ChildContent ), new RenderFragment( RenderWithLayout ) );
        builder.CloseComponent();
    }

    private void RenderWithLayout( RenderTreeBuilder builder )
    {
        builder.OpenComponent<AppLayout>( 0 );
        builder.AddComponentParameter( 1, nameof( AppLayout.ChildContent ), new RenderFragment( base.Render ) );
        builder.CloseComponent();
    }
}