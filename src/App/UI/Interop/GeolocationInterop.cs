using Microsoft.JSInterop;

namespace Wadio.App.UI.Interop;

internal sealed class GeolocationInterop( IJSRuntime runtime ) : Interop( runtime, "Geolocation" )
{
    public ValueTask<GeolocationPosition> GetCurrentPosition( PositionOptions? options = default, CancellationToken cancellation = default ) => Access( async ( module, cancellation ) =>
    {
        var completion = new TaskCompletionSource<GeolocationPosition>();

        using var resolve = new CallbackReference<GeolocationPosition>( location =>
        {
            completion.SetResult( location );
            return default;
        } );

        using var reject = new CallbackReference<GeolocationError>( error =>
        {
            completion.SetException( new GeolocationException( error ) );
            return default;
        } );

        await module.InvokeVoidAsync(
            "getCurrentPosition",
            cancellation,
            resolve.Reference,
            reject.Reference,
            options );

        return await completion.Task;
    }, cancellation );
}

internal sealed record GeolocationPosition
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public static implicit operator Coordinate( GeolocationPosition coords ) => (coords.Latitude, coords.Longitude);
};

internal sealed record GeolocationError( GeolocationErrorCode Code, string? Message );
internal enum GeolocationErrorCode
{
    PermissionDenied = 1,
    PositionUnavailable = 2,
    Timeout = 3,
}

internal sealed class GeolocationException( GeolocationError error ) : Exception( error.Message )
{
    public GeolocationErrorCode Code => error.Code;
}

internal sealed record PositionOptions
{
    public bool? EnableHighAccuracy { get; init; }
    public double? Timeout { get; init; }
    public double? MaximumAge { get; init; }
}