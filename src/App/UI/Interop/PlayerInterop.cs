using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Interop;

internal sealed class PlayerInterop( IJSRuntime runtime ) : Interop( runtime, "Player" )
{
    public async ValueTask<StationPlayer> CreatePlayer( StationPlayerOptions options, StationPlayerEvents? events = default, CancellationToken cancellation = default ) => await Access<StationPlayer>( async ( module, cancellation ) =>
    {
        var eventsRef = new PlayerEventsReference( events );
        try
        {
            var playerRef = await module.InvokeAsync<IJSObjectReference>(
                "createPlayer",
                cancellation,
                options,
                eventsRef.Reference );

            return new( eventsRef, playerRef );
        }
        catch
        {
            eventsRef.Dispose();
            throw;
        }
    }, cancellation );
}

internal sealed class StationPlayer( PlayerEventsReference events, IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        events.Dispose();
    }

    public ValueTask Play( Station station, StationPlayerOptions options, CancellationToken cancellation = default ) => reference.InvokeVoidAsync( "play", cancellation, station, options );

    public ValueTask<bool> Muted( bool value ) => reference.InvokeAsync<bool>( "muted", value );

    public ValueTask Stop( CancellationToken cancellation = default ) => reference.InvokeVoidAsync( "stop", cancellation );

    public ValueTask<float> Volume( float value, CancellationToken cancellation = default ) => reference.InvokeAsync<float>( "volume", cancellation, value );
}

internal sealed class StationPlayerEvents
{
    public Func<OnMetaChangedEvent, ValueTask> OnMetaChanged { get; init; } = static _ => ValueTask.CompletedTask;
    public Func<ValueTask> OnStop { get; init; } = static ( ) => ValueTask.CompletedTask;
}

internal sealed record StationPlayerOptions( bool Muted, float Volume );

internal sealed class PlayerEventsReference : IDisposable
{
    private readonly StationPlayerEvents? events;

    public DotNetObjectReference<PlayerEventsReference> Reference { get; }

    [DynamicDependency( nameof( OnMetaChanged ) )]
    [DynamicDependency( nameof( OnStop ) )]
    public PlayerEventsReference( StationPlayerEvents? events )
    {
        this.events = events;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task OnMetaChanged( OnMetaChangedEvent e )
    {
        if( events is not null )
        {
            await events.OnMetaChanged( e );
        }
    }

    [JSInvokable]
    public async Task OnStop( )
    {
        if( events is not null )
        {
            await events.OnStop();
        }
    }
}

public sealed record class MediaMetadata
{
    public string? Album { get; init; }
    public string? Artist { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public ImmutableArray<MediaImage> Artwork { get; init; } = [];
    public string? Title { get; init; }
}

public sealed record MediaImage
{
    public string Type { get; init; }

    [JsonPropertyName( "src" )]
    public Uri Url { get; init; }
}

public sealed record OnMetaChangedEvent
{
    public Guid? StationId { get; init; }

    [JsonPropertyName( "meta" )]
    public MediaMetadata? Metadata { get; init; }
}