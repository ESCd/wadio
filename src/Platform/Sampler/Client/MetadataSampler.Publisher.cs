using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Open.ChannelExtensions;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Abstractions.Json;

namespace Wadio.Platform.Sampler.Client;

internal sealed class MetadataSamplePublisher(
    IHttpClientFactory clientFactory,
    ILogger<MetadataSamplePublisher> logger,
    Channel<MetadataSample> queue ) : BackgroundService
{
    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        _ = await queue.ReadAllConcurrentlyAsync(
            Environment.ProcessorCount,
            sample => OnProcess( sample, cancellation ),
            cancellation );
    }

    private async ValueTask OnProcess( MetadataSample sample, CancellationToken cancellation )
    {
        using var http = clientFactory.CreateClient( nameof( MetadataSample ) );
        try
        {
            using var response = await http.PostAsJsonAsync(
                default( Uri? ),
                sample,
                SamplerJsonContext.Default.MetadataSample,
                cancellation );

            response.EnsureSuccessStatusCode();
        }
        catch( Exception e )
        {
            logger.OnFailedToProcess( sample, e );
        }
    }

    public override async Task StopAsync( CancellationToken cancellation )
    {
        await queue.CompleteAsync();
        await base.StopAsync( cancellation );
    }
}

internal static partial class MetadataSamplerProcessorLogging
{
    [LoggerMessage( Level = LogLevel.Error, Message = "Failed to process sample: {sample}" )]
    public static partial void OnFailedToProcess( this ILogger<MetadataSamplePublisher> logger, MetadataSample sample, Exception? exception );
}