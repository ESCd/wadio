using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Wadio.App.UI.Infrastructure.Markdown;

internal sealed class ExternalLinksExtension : IMarkdownExtension
{
    public void Setup( MarkdownPipelineBuilder pipeline )
    {
    }

    public void Setup( MarkdownPipeline pipeline, IMarkdownRenderer renderer )
    {
        if( renderer is HtmlRenderer htmlRenderer )
        {
            var linkRenderer = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
            if( linkRenderer is not null )
            {
                linkRenderer.TryWriters.Remove( TryRenderLink );
                linkRenderer.TryWriters.Add( TryRenderLink );
            }
        }
    }

    private static bool TryRenderLink( HtmlRenderer renderer, LinkInline link )
    {
        link.GetAttributes().AddPropertyIfNotExist( "target", "_blank" );
        return false;
    }
}