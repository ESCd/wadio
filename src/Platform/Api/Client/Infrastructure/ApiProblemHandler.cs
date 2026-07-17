using System.Net;
using System.Net.Http.Json;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Api.Abstractions.Json;

namespace Wadio.Platform.Api.Client.Infrastructure;

internal sealed class ApiProblemHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellation )
    {
        var response = await base.SendAsync( request, cancellation );
        try
        {
            return response.EnsureSuccessStatusCode();
        }
        catch( HttpRequestException exception )
        {
            if( response.StatusCode >= HttpStatusCode.BadRequest )
            {
                var problem = await response.Content.ReadFromJsonAsync( ApiJsonContext.Default.ApiProblem, cancellation );
                throw new ApiProblemException( exception, problem! );
            }
        }

        return response;
    }
}