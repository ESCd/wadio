using Microsoft.AspNetCore.SignalR;
using Wadio.App.Abstractions.Api;

namespace Wadio.App.Web.Hubs;

public sealed class MetadataHub(
    IWadioApi api,
    IMetadataWorkerContext worker ) : Hub
{
    public const string StationIdParameter = "stationId";
    private static readonly object SubscriptionKey = new();

    public override async Task OnConnectedAsync( )
    {
        if( !(Context.GetHttpContext()?.Request.Query.TryGetValue( StationIdParameter, out var values ) is true && Guid.TryParse( values.FirstOrDefault(), out var stationId )) )
        {
            throw FailedToConnect( new ArgumentNullException( StationIdParameter, $"The '{StationIdParameter}' parameter is required." ) );
        }

        await Unsubscribe( Context );

        var station = await api.Stations.Get(
            stationId,
            Context.ConnectionAborted ) ?? throw FailedToConnect( new ArgumentException( $"Station '{stationId}' does not exist.", StationIdParameter ) );

        if( station.IsHls )
        {
            throw FailedToConnect( new ArgumentException( $"Station '{stationId}' is not supported. (IsHls=true)", StationIdParameter ) );
        }

        var subscription = await worker.Subscribe( stationId, Context.ConnectionAborted );
        if( !Context.Items.TryAdd( SubscriptionKey, subscription ) )
        {
            await subscription.DisposeAsync();
            throw FailedToConnect( new ArgumentException( "A subscription already exists." ) );
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            stationId.ToString(),
            Context.ConnectionAborted );

        static HubException FailedToConnect( ArgumentException exception ) => new( exception.Message, exception );
    }

    public override async Task OnDisconnectedAsync( Exception? exception ) => await Unsubscribe( Context );

    private async ValueTask Unsubscribe( HubCallerContext context )
    {
        ArgumentNullException.ThrowIfNull( context );

        if( context.Items.Remove( SubscriptionKey, out var value ) && value is IMetadataWorkerSubscription subscription )
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                subscription.StationId.ToString() );

            await subscription.DisposeAsync();
        }
    }
}