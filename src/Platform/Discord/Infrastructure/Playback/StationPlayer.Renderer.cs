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
    ILogger<StationPlayerRenderer> logger,
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
        await using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
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
                await using( cancellation.Register( ( ) => request.Completion.TrySetCanceled( cancellation ) ) )
                {
                    var result = await OnRender(
                        await contextFactory.Create(),
                        logger,
                        request,
                        cancellation );

                    request.Completion.TrySetResult( result );
                }
            }
            catch( Exception e )
            {
                request.Completion.TrySetException( e );
            }
        }

        static async ValueTask<StationPlayerRenderResult> OnRender(
            ComponentCreationContext context,
            ILogger<StationPlayerRenderer> logger,
            StationPlayerRenderRequest request,
            CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( context );
            ArgumentNullException.ThrowIfNull( logger );
            ArgumentNullException.ThrowIfNull( request );

            _ = await request.Messages.ToChannel( request.Messages.Count, false, false, cancellation )
                .PipeFilterAsync(
                    out var stale,
                    request.Messages.Count,
                    Environment.ProcessorCount,
                    message => Render( context, message, request.Status, cancellation ),
                    cancellation )
                .ReadAllConcurrently( Environment.ProcessorCount, message => logger.OnRenderedPlayer( message.Id ), cancellation );

            return new( await stale.ToListAsync( stale.Count ) );

            static async ValueTask<bool> Render( ComponentCreationContext context, RestMessage message, StationPlayerStatus? status, CancellationToken cancellation )
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

internal static partial class StationPlayerRendererLogging
{
    [LoggerMessage( LogLevel.Trace, Message = "({messageId}) Rendered Player Output" )]
    public static partial void OnRenderedPlayer( this ILogger<StationPlayerRenderer> logger, ulong messageId );
}

internal sealed record StationPlayerRenderRequest( IReadOnlyCollection<RestMessage> Messages, StationPlayerStatus? Status )
{
    public TaskCompletionSource<StationPlayerRenderResult> Completion { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
}

internal sealed record StationPlayerRenderResult( IReadOnlyCollection<RestMessage> Stale );