using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class MapInterop( IJSRuntime runtime ) : Interop( runtime, "Map" )
{
    public ValueTask<MapReference> CreateMap( ElementReference element, MapOptions options, MapEvents? events = default, CancellationToken cancellation = default ) => Access<MapReference>( async ( module, cancellation ) =>
    {
        ArgumentNullException.ThrowIfNull( options );

        var eventsRef = new MapEventsReference( events );
        try
        {
            var mapRef = await module.InvokeAsync<IJSObjectReference>(
                "createMap",
                cancellation,
                element,
                options,
                eventsRef.Reference );

            return new( eventsRef, mapRef );
        }
        catch
        {
            eventsRef.Dispose();
            throw;
        }
    }, cancellation );
}

public sealed record Coordinate
{
    [JsonPropertyName( "lat" )]
    public double Latitude { get; init; }

    [JsonPropertyName( "lng" )]
    public double Longitude { get; init; }

    public Coordinate( double latitude, double longitude )
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static implicit operator Coordinate( (double Latitude, double Longitude) coordinate ) => new( coordinate.Latitude, coordinate.Longitude );
    public static implicit operator (double Latitude, double Longitude)( Coordinate coordinate ) => (coordinate.Latitude, coordinate.Longitude);
}

internal sealed class MapEvents
{
    public Func<OnBoundsChangedEvent, ValueTask> OnBoundsChanged { get; init; } = _ => ValueTask.CompletedTask;
    public Func<OnReadyEvent, ValueTask> OnReady { get; init; } = _ => ValueTask.CompletedTask;
}

internal sealed class MapEventsReference : IDisposable
{
    private readonly MapEvents? events;

    public DotNetObjectReference<MapEventsReference> Reference { get; }

    [DynamicDependency( nameof( OnBoundsChanged ) )]
    [DynamicDependency( nameof( OnReady ) )]
    public MapEventsReference( MapEvents? events )
    {
        this.events = events;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task OnBoundsChanged( OnBoundsChangedEvent e )
    {
        if( events is not null )
        {
            await events.OnBoundsChanged( e );
        }
    }

    [JSInvokable]
    public async Task OnReady( OnReadyEvent e )
    {
        if( events is not null )
        {
            await events.OnReady( e );
        }
    }
}

internal sealed record MapOptions( Coordinate Center, bool EnableLocate = true )
{
    public uint BufferSize { get; init; } = 2;
    public bool Dragging { get; init; } = true;
    public float? MaxZoom { get; init; } = 15;
    public float? MinZoom { get; init; }
    public float? Zoom { get; init; } = 15;
}

internal sealed class MapReference( MapEventsReference events, IJSObjectReference map ) : IAsyncDisposable
{
    private readonly ConcurrentBag<MarkerReference> markers = [];

    public async ValueTask DisposeAsync( )
    {
        try
        {
            await DisposeMarkers( markers );

            await map.InvokeVoidAsync( "dispose" );
            await map.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }

        events.Dispose();

        static async ValueTask DisposeMarkers( ConcurrentBag<MarkerReference> markers )
        {
            ArgumentNullException.ThrowIfNull( markers );

            foreach( var marker in markers.Cast<IMarkerDisposable>() )
            {
                await marker.DisposeAsync();
            }

            markers.Clear();
        }
    }

    public async ValueTask<MarkerReference> AddMarker( MarkerOptions options, MarkerEvents events, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( options );

        var eventsRef = new MarkerEventsReference( events );
        try
        {
            if( markers.TryTake( out var marker ) )
            {
                await marker.Update( options, eventsRef, cancellation );
                return marker;
            }

            var reference = await map.InvokeAsync<IJSObjectReference>(
                "addMarker",
                cancellation,
                options,
                eventsRef.Reference );

            return new( eventsRef, reference, markers );
        }
        catch
        {
            eventsRef.Dispose();
            throw;
        }
    }

    public ValueTask Refresh( ) => map.InvokeVoidAsync( "refresh" );
}

file interface IMarkerDisposable
{
    public ValueTask DisposeAsync( );
}

internal sealed class MarkerEvents
{
    public Func<ValueTask> OnPopupClosed { get; init; } = ( ) => ValueTask.CompletedTask;
    public Func<ValueTask> OnPopupOpen { get; init; } = ( ) => ValueTask.CompletedTask;
}

internal sealed class MarkerEventsReference : IDisposable
{
    private readonly MarkerEvents? events;

    public DotNetObjectReference<MarkerEventsReference> Reference { get; }

    [DynamicDependency( nameof( OnPopupOpen ) )]
    public MarkerEventsReference( MarkerEvents? events )
    {
        this.events = events;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task OnPopupClosed( )
    {
        if( events is not null )
        {
            await events.OnPopupClosed();
        }
    }

    [JSInvokable]
    public async Task OnPopupOpen( )
    {
        if( events is not null )
        {
            await events.OnPopupOpen();
        }
    }
}

internal sealed record MarkerOptions( Coordinate Position )
{
    public bool AutoPopup { get; init; }
    public bool CloseButton { get; init; } = true;
    public MarkerStyle Style { get; init; }
    public string? Title { get; init; }
}

internal sealed class MarkerReference( MarkerEventsReference events, IJSObjectReference marker, ConcurrentBag<MarkerReference> pool ) : IAsyncDisposable, IMarkerDisposable
{
    private MarkerEventsReference? events = events;

    public async ValueTask DisposeAsync( )
    {
        await marker.InvokeVoidAsync( "reset" );

        pool.Add( this );
    }

    async ValueTask IMarkerDisposable.DisposeAsync( )
    {
        try
        {
            await marker.InvokeVoidAsync( "dispose" );
            await marker.DisposeAsync();
        }
        catch( JSDisconnectedException ) { }

        if( events is not null )
        {
            events.Dispose();
            events = default!;
        }
    }

    public ValueTask SetPopupContent( ElementReference? content, CancellationToken cancellation = default ) => marker.InvokeVoidAsync(
        "setPopupContent",
        cancellation,
        content );

    public ValueTask Update( MarkerOptions options, MarkerEventsReference events, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( options );
        ArgumentNullException.ThrowIfNull( events );

        if( this.events is not null )
        {
            this.events.Dispose();
            this.events = default;
        }

        this.events = events;
        return marker.InvokeVoidAsync(
            "update",
            cancellation,
            options,
            events.Reference );
    }
}

public enum MarkerStyle : byte
{
    Default,
    Custom,
}

public sealed record OnBoundsChangedEvent( Coordinate Center, double Radius );
public sealed record OnPopupOpenEvent( );
public sealed record OnReadyEvent( Coordinate Center, double Radius );