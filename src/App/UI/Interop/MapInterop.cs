using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class MapInterop( IJSRuntime runtime ) : Interop( runtime, "Map" )
{
    public ValueTask<MapReference> CreateMap( ElementReference element, (double Latitude, double Longitude) coordinate, CancellationToken cancellation = default ) => Access<MapReference>( async ( module, cancellation ) =>
    {
        var reference = await module.InvokeAsync<IJSObjectReference>(
            "createMap",
            cancellation,
            element,
            new { coordinate.Latitude, coordinate.Longitude } );

        return new( reference );
    }, cancellation );
}

internal sealed class MapReference( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }

    public ValueTask Refresh( ) => reference.InvokeVoidAsync( "refresh" );
}