using System.Net;
using System.Net.Http.Json;
using Wadio.App.UI.Abstractions;
using Wadio.App.UI.Json;

namespace Wadio.App.UI.Infrastructure;

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
                var problem = await response.Content.ReadFromJsonAsync( AppJsonContext.Default.ApiProblem, cancellation );
                throw new ApiProblemException( exception, problem! );
            }
        }

        return response;
    }
}