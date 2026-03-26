using System.Net;
using System.Threading.Channels;
using NetCord;
using NetCord.Rest;
using Open.ChannelExtensions;
using Wadio.Platform.Discord.Abstractions;
using Wadio.Platform.Discord.Interactions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class StationPlayerRenderer(
    IComponentContextFactory contextFactory,
    Channel<StationPlayerRenderRequest> queue ) : BackgroundService
{
    public async ValueTask<StationPlayerRenderResult> Render( IReadOnlyCollection<RestMessage> messages, StationPlayerStatus? status, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( messages );

        if( messages.Count is 0 )
        {
            return new( [] );
        }

        var request = new StationPlayerRenderRequest( messages, status );
        using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
        {
            await queue.Writer.WriteAsync( request, cancellation );
            return await request.Completion.Task.ConfigureAwait( false );
        }
    }

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        while( !cancellation.IsCancellationRequested )
        {
            var request = await queue.Reader.ReadAsync( cancellation );
            try
            {
                using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
                {
                    var result = await OnRender(
                        request,
                        await contextFactory.Create(),
                        cancellation );

                    request.Completion.TrySetResult( result );
                }
            }
            catch( Exception e )
            {
                request.Completion.TrySetException( e );
            }
        }

        static async ValueTask<StationPlayerRenderResult> OnRender( StationPlayerRenderRequest request, ComponentCreationContext context, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( request );
            ArgumentNullException.ThrowIfNull( context );

            _ = await request.Messages.ToChannel( request.Messages.Count, false, false, cancellation )
                .PipeFilterAsync(
                    out var stale,
                    request.Messages.Count,
                    Environment.ProcessorCount,
                    message => Render( message, request.Status, context, cancellation ),
                    cancellation )
                .ToListAsync( request.Messages.Count );

            return new( await stale.ToListAsync( stale.Count ) );

            static async ValueTask<bool> Render( RestMessage message, StationPlayerStatus? status, ComponentCreationContext context, CancellationToken cancellation )
            {
                ArgumentNullException.ThrowIfNull( message );
                ArgumentNullException.ThrowIfNull( context );

                try
                {
                    await message.ModifyAsync( edit =>
                    {
                        edit.Components = [ PlayerComponent.Create( context, status ) ];
                        edit.Flags = MessageFlags.IsComponentsV2;
                    }, default, cancellation );
                }
                catch( RestException e ) when( e.StatusCode is HttpStatusCode.NotFound )
                {
                    return false;
                }

                return true;
            }
        }
    }
}

internal sealed record StationPlayerRenderRequest( IReadOnlyCollection<RestMessage> Messages, StationPlayerStatus? Status )
{
    public TaskCompletionSource<StationPlayerRenderResult> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
}

internal sealed record StationPlayerRenderResult( IReadOnlyCollection<RestMessage> Stale );