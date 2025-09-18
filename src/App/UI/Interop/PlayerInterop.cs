using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.UI.Interop;

internal sealed class PlayerInterop( IJSRuntime runtime ) : Interop( runtime, "Player" )
{
    public async ValueTask<PlayerAudio> CreateAudio( PlayerAudioOptions options, Func<string, ValueTask>? onTitleChanged = default, CancellationToken cancellation = default ) => await Access( async ( module, cancellation ) =>
    {
        var callback = new PlayerTitleCallback( onTitleChanged ?? (_ => default) );
        var reference = await module.InvokeAsync<IJSObjectReference>(
            "createAudio",
            cancellation,
            options,
            callback.Reference );

        return new PlayerAudio( callback, reference );
    }, cancellation );
}

internal sealed class PlayerAudio( PlayerTitleCallback callback, IJSObjectReference reference ) : IAsyncDisposable
{
    public async ValueTask DisposeAsync( )
    {
        await reference.InvokeVoidAsync( "dispose" );
        await reference.DisposeAsync();

        callback.Dispose();
    }

    public ValueTask Play( Station station, PlayerAudioOptions options ) => reference.InvokeVoidAsync( "play", station, options );

    public ValueTask<bool> Muted( bool value ) => reference.InvokeAsync<bool>( "muted", value );

    public ValueTask Stop( ) => reference.InvokeVoidAsync( "stop" );

    public ValueTask<float> Volume( float value ) => reference.InvokeAsync<float>( "volume", value );
}

internal sealed record PlayerAudioOptions( bool Muted, float Volume );

internal sealed class PlayerTitleCallback : IDisposable
{
    private readonly Func<string, ValueTask> callback;

    public DotNetObjectReference<PlayerTitleCallback> Reference { get; }

    [DynamicDependency( nameof( Invoke ) )]
    public PlayerTitleCallback( Func<string, ValueTask> callback )
    {
        this.callback = callback;
        Reference = DotNetObjectReference.Create( this );
    }

    public void Dispose( ) => Reference.Dispose();

    [JSInvokable]
    public async Task Invoke( string title ) => await callback( title );
}