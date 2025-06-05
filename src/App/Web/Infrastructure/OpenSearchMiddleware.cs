using System.Text;

namespace Wadio.App.Web.Infrastructure;

internal static class OpenSearchMiddleware
{
    public static IEndpointConventionBuilder MapOpenSearch( this WebApplication app )
    {
        ArgumentNullException.ThrowIfNull( app );

        return app.MapGet( "/osd.xml", async ( HttpContext context, CancellationToken cancellation ) =>
        {
            var url = new UriBuilder
            {
                Host = context.Request.Host.Host,
                Scheme = context.Request.Scheme,
            };

            if( context.Request.Host.Port.HasValue )
            {
                url.Port = context.Request.Host.Port.Value;
            }

            context.Response.GetTypedHeaders()
                .ContentType = new( "application/opensearchdescription+xml" );

            await context.Response.WriteAsync( @$"<OpenSearchDescription xmlns=""http://a9.com/-/spec/opensearch/1.1/"" xmlns:moz=""http://www.mozilla.org/2006/browser/search/"">
  <ShortName>Wadio</ShortName>
  <Description>A music app, powered by radio-browser.</Description>
  <InputEncoding>[UTF-8]</InputEncoding>

  <Image type=""image/svg+xml"">{url}/icon.svg</Image>
  <Image width=""16"" height=""16"" type=""image/png"">{url}/icon-16.png</Image>
  <Image width=""24"" height=""24"" type=""image/png"">{url}/icon-24.png</Image>
  <Image width=""48"" height=""48"" type=""image/png"">{url}/icon-48.png</Image>
  <Image width=""64"" height=""64"" type=""image/png"">{url}/icon-64.png</Image>
  <Image width=""96"" height=""96"" type=""image/png"">{url}/icon-96.png</Image>
  <Image width=""128"" height=""128"" type=""image/png"">{url}/icon-128.png</Image>
  <Image width=""154"" height=""154"" type=""image/png"">{url}/icon-64.png</Image>
  <Image width=""256"" height=""256"" type=""image/png"">{url}/icon-256.png</Image>
  <Image width=""512"" height=""512"" type=""image/png"">{url}/icon-512.png</Image>
  <Tags>music radio-browser radiobrowser radiobrowser-api</Tags>

  <Url type=""text/html"" template=""{url}/search?Name={{searchTerms}}&amp;Count={{count}}"" />
</OpenSearchDescription>", Encoding.UTF8, cancellation );
        } );
    }
}