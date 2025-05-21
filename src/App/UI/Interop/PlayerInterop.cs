using Microsoft.JSInterop;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.UI.Interop;

internal sealed class PlayerInterop( IJSRuntime runtime ) : Interop( runtime, "Player" )
{
    public async ValueTask<PlayerAudio> CreateAudio( PlayerAudioOptions options, CancellationToken cancellation = default ) => new(
        await Access( ( module, cancellation ) => module.InvokeAsync<IJSObjectReference>( "createAudio", cancellation, options ), cancellation ) );
}

internal sealed class PlayerAudio( IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();
    }

    public ValueTask Play( Station station, PlayerAudioOptions options ) => reference.InvokeVoidAsync( "play", station, options );

    public ValueTask<bool> Muted( bool value ) => reference.InvokeAsync<bool>( "muted", value );

    public ValueTask Stop( ) => reference.InvokeVoidAsync( "stop" );

    public ValueTask<float> Volume( float value ) => reference.InvokeAsync<float>( "volume", value );
}

internal sealed record PlayerAudioOptions( bool Muted, float Volume );