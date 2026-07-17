using System.Threading.Channels;
using Open.ChannelExtensions;
using Wadio.Platform.Sampler.Abstractions;

namespace Wadio.Platform.Sampler.Infrastructure;

internal sealed class MetadataSampleWriter(
    SamplerDbContext db,
    ILogger<MetadataSampleWriter> logger ) : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly Channel<MetadataSampleAction> queue = Channel.CreateBounded<MetadataSampleAction>( new BoundedChannelOptions( Environment.ProcessorCount * 4 )
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = false,
        SingleWriter = false,
    } );

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        _ = await queue.ReadAllConcurrentlyAsync(
            Environment.ProcessorCount,
            sample => OnWriteSample( sample, cancellation ),
            cancellation );

        async ValueTask OnWriteSample( MetadataSampleAction action, CancellationToken cancellation )
        {
            try
            {
                _ = await db.Meta.InsertAsync( action.Sample, cancellation );
            }
            catch( Exception e ) when( action.Attempt is MaxRetries )
            {
                logger.OnFailedToWrite( action.Attempt, action.Sample, e );
                return;
            }
            catch
            {
                await queue.Writer.WriteAsync(
                    action with { Attempt = action.Attempt + 1 },
                    cancellation );
            }
        }
    }

    public override async Task StopAsync( CancellationToken cancellation )
    {
        await queue.CompleteAsync();
        await base.StopAsync( cancellation );
    }

    public ValueTask Write( MetadataSample sample, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( sample );

        return queue.Writer.WriteAsync( new( sample ), cancellation );
    }

    private sealed record MetadataSampleAction( MetadataSample Sample )
    {
        public int Attempt { get; init; } = 1;
    }
}

internal static partial class MetadataSampleWriterLogging
{
    [LoggerMessage( LogLevel.Error, "Failed to write Sample after {attempts} attempts: {sample}" )]
    public static partial void OnFailedToWrite( this ILogger<MetadataSampleWriter> logger, int attempts, MetadataSample sample, Exception? exception );
}
