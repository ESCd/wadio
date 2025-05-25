using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class MapInterop( IJSRuntime runtime ) : Interop( runtime, "Map" )
{
    public async ValueTask<MapReference> CreateMap( ElementReference element, (double Latitude, double Longitude) coordinate, CancellationToken cancellation = default )
    {
        var reference = await Access(
            ( module, cancellation ) => module.InvokeAsync<IJSObjectReference>( "createMap", cancellation, element, new { coordinate.Latitude, coordinate.Longitude } ),
            cancellation );

        return new( reference );
    }
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