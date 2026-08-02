using Open.ChannelExtensions;
using Wadio.Extensions.CloudflareApi.Abstractions;

namespace Wadio.Platform.Api.Infrastructure;

internal sealed class EnforceThumbnailQuotas( ICloudflareImagesApi cloudflare ) : BackgroundService
{
    private static readonly TimeSpan EnforceDelay = TimeSpan.FromMinutes( 15 );
    private const int QuotaThreshold = 250;

    protected override async Task ExecuteAsync( CancellationToken cancellation )
    {
        while( !cancellation.IsCancellationRequested )
        {
            var (_, _, stats) = await cloudflare.StatsAsync( cancellation );
            if( stats?.Count is StatCount count )
            {
                var (allowed, current) = count;
                if( current >= allowed || allowed - current < QuotaThreshold )
                {
                    var (_, _, result) = await cloudflare.ListAsync( new()
                    {
                        PerPage = QuotaThreshold,
                        Sort = SortOrder.Asc
                    }, cancellation );

                    if( result?.Images?.Count > 0 )
                    {
                        await result.Images.ToChannel( result.Images.Count, cancellationToken: cancellation )
                            .Transform( image => image.Id )
                            .Filter( imageId => !string.IsNullOrWhiteSpace( imageId ) )
                            .ReadAllConcurrentlyAsync(
                                Environment.ProcessorCount,
                                async imageId => await cloudflare.DeleteAsync( imageId!, cancellation ),
                                cancellation );
                    }
                }
            }

            await Task.Delay( EnforceDelay, cancellation );
        }
    }
}