using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class MarqueeInterop( IJSRuntime runtime ) : Interop( runtime, "Marquee" )
{
    public async ValueTask<MarqueeReference> Attach( ElementReference target, ElementReference parent, CancellationToken cancellation = default )
    {
        var reference = await Access(
            ( module, cancellation ) => module.InvokeAsync<IJSObjectReference>( "attach", cancellation, target, parent ),
            cancellation );

        return new MarqueeReference( reference );
    }
}

internal sealed record MarqueeMeasurement(
    double InnerWidth,
    [property: JsonPropertyName( "overflowing" )] bool IsOverflowing,
    double OuterWidth );

internal sealed class MarqueeReference( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }

    public ValueTask<MarqueeMeasurement?> Measure( CancellationToken cancellation = default ) => reference.InvokeAsync<MarqueeMeasurement?>( "measure", cancellation );
}