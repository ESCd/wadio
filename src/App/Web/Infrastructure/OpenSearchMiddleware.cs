using System.Text;
using Microsoft.AspNetCore.Mvc;
using Wadio.App.UI.Abstractions;

namespace Wadio.App.Web.Infrastructure;

internal static class OpenSearchMiddleware
{
    public static RouteGroupBuilder MapOpenSearch( this WebApplication app )
    {
        ArgumentNullException.ThrowIfNull( app );

        var osd = app.MapGroup( "/osd" )
            .AllowAnonymous()
            .ExcludeFromDescription();

        osd.MapGet( "/spec.xml", Spec );
        osd.MapGet( "/suggest", Suggest );

        return osd;
    }

    private static async Task Spec( HttpContext context, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( context );

        var url = context.Request.GetBaseUrl();

        context.Response.GetTypedHeaders().ContentType = new( "application/opensearchdescription+xml" );
        await context.Response.WriteAsync( @$"<OpenSearchDescription xmlns=""http://a9.com/-/spec/opensearch/1.1/"" xmlns:moz=""http://www.mozilla.org/2006/browser/search/"">
  <ShortName>Wadio</ShortName>
  <Description>A music app, powered by radio-browser.</Description>
  <InputEncoding>[UTF-8]</InputEncoding>

  <Image type=""image/svg+xml"">{url}icon.svg</Image>
  <Image width=""16"" height=""16"" type=""image/png"">{url}icon-16.png</Image>
  <Image width=""24"" height=""24"" type=""image/png"">{url}icon-24.png</Image>
  <Image width=""48"" height=""48"" type=""image/png"">{url}icon-48.png</Image>
  <Image width=""64"" height=""64"" type=""image/png"">{url}icon-64.png</Image>
  <Image width=""96"" height=""96"" type=""image/png"">{url}icon-96.png</Image>
  <Image width=""128"" height=""128"" type=""image/png"">{url}icon-128.png</Image>
  <Image width=""154"" height=""154"" type=""image/png"">{url}icon-64.png</Image>
  <Image width=""256"" height=""256"" type=""image/png"">{url}icon-256.png</Image>
  <Image width=""512"" height=""512"" type=""image/png"">{url}icon-512.png</Image>
  <Tags>music radio-browser radiobrowser radiobrowser-api</Tags>

  <Url type=""text/html"" template=""{url}search?Name={{searchTerms}}&amp;Count={{count}}"" />
  <Url type=""application/x-suggestions+json"" template=""{url}osd/suggest?q={{searchTerms}}&count={{count}}"" />

  <Url type=""application/opensearchdescription+xml"" rel=""self"" template=""{url}osd/spec.xml"" />
</OpenSearchDescription>", Encoding.UTF8, cancellation );
    }

    private static async Task<IResult> Suggest( [FromServices] IWadioApi api, [FromQuery( Name = "q" )] string query, [FromQuery] uint count = 5, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( api );

        if( string.IsNullOrWhiteSpace( query ) )
        {
            return TypedResults.Ok<string[]>( [ query ] );
        }

        var names = await api.Stations.Search( new()
        {
            Count = count,
            Name = query,
            Order = StationOrderBy.Name,
        }, cancellation ).Select( station => station.Name ).ToArrayAsync( cancellation );

        return TypedResults.Ok<object[]>( [ query, names ] );
    }
}

static file class HttpRequestExtensions
{
    public static Uri GetBaseUrl( this HttpRequest request )
    {
        ArgumentNullException.ThrowIfNull( request );

        var url = new UriBuilder
        {
            Host = request.Host.Host,
            Scheme = request.Scheme,
        };

        if( request.Host.Port.HasValue )
        {
            url.Port = request.Host.Port.Value;
        }

        return url.Uri;
    }
}