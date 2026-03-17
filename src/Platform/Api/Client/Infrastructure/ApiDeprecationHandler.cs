using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Api.Client.Infrastructure;

internal sealed class ApiDeprecationHandler(
    ILogger<IWadioApi> logger,
    ObjectPool<StringBuilder> builders ) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( request );

        var response = await base.SendAsync( request, cancellation );
        if( response?.Headers.TryGetValues( ApiHeaderNames.Deprecated, out var values ) is true )
        {
            var builder = builders.Get();
            try
            {
                logger.WarnDeprecatedEndpoint(
                    request.RequestUri,
                    values.Aggregate(
                        builder,
                        ( content, value ) => content.AppendLine( value ) ).ToString().Trim() );

            }
            finally
            {
                builders.Return( builder );
            }
        }

        return response!;
    }
}

internal static partial class OverBoardApiLogging
{
    [LoggerMessage( 0, LogLevel.Warning, "Endpoint '{requestUri}' has been marked as deprecated: '{message}'" )]
    public static partial void WarnDeprecatedEndpoint( this ILogger<IWadioApi> logger, Uri? requestUri, string message );
}
