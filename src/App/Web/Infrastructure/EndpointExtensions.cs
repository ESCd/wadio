using Microsoft.AspNetCore.Mvc;

namespace Wadio.App.Web.Infrastructure;

public static class EndpointExtensions
{
    public static bool IsApiEndpoint( this Endpoint endpoint )
    {
        ArgumentNullException.ThrowIfNull( endpoint );
        return endpoint.Metadata.GetMetadata<ApiControllerAttribute>() is not null;
    }
}