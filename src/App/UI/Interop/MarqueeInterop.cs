using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class MarqueeInterop( IJSRuntime runtime ) : Interop( runtime, "Marquee" )
{
    public ValueTask<MarqueeReference> Attach( ElementReference target, ElementReference parent, CancellationToken cancellation = default ) => Access( ( module, cancellation ) =>
    {
#pragma warning disable IL2026
        var reference = module.Invoke<IJSInProcessObjectReference>(
            "attach",
            target,
            parent );
#pragma warning restore IL2026

        return ValueTask.FromResult<MarqueeReference>( new( reference ) );
    }, cancellation );
}

internal sealed record MarqueeMeasurement(
    double InnerWidth,
    [property: JsonPropertyName( "overflowing" )] bool IsOverflowing,
    double OuterWidth );

internal sealed class MarqueeReference( IJSInProcessObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        try
        {
            reference.InvokeVoid( "dispose" );
            await reference.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }
    }

    public ValueTask<MarqueeMeasurement?> Measure( CancellationToken cancellation = default ) => reference.InvokeAsync<MarqueeMeasurement?>( "measure", cancellation );
}