using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Wadio.App.UI.Infrastructure.Markdown;

internal sealed class ShiftHeadingExtension( int offset ) : IMarkdownExtension
{
    private readonly int offset = offset;

    public void Setup( MarkdownPipelineBuilder pipeline )
    {
    }

    public void Setup( MarkdownPipeline pipeline, IMarkdownRenderer renderer )
    {
        if( renderer is HtmlRenderer htmlRenderer )
        {
            var headingRenderer = htmlRenderer.ObjectRenderers.FindExact<HeadingRenderer>();
            if( headingRenderer is not null )
            {
                headingRenderer.TryWriters.Remove( TryRenderHeading );
                headingRenderer.TryWriters.Add( TryRenderHeading );
            }
        }
    }

    private bool TryRenderHeading( HtmlRenderer renderer, HeadingBlock block )
    {
        var heading = $"h{block.Level + offset}";
        if( renderer.EnableHtmlForBlock )
        {
            renderer.Write( '<' );
            WriteRaw( renderer, heading );

            renderer.WriteAttributes( block );
            WriteRaw( renderer, '>' );
        }

        renderer.WriteLeafInline( block );
        if( renderer.EnableHtmlForBlock )
        {
            renderer.Write( "</" );
            WriteRaw( renderer, heading );

            renderer.WriteLine( '>' );
        }

        renderer.EnsureLine();
        return true;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    internal static void WriteRaw( HtmlRenderer renderer, ReadOnlySpan<char> content ) => renderer.Writer.Write( content );

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    internal static void WriteRaw( HtmlRenderer renderer, char content ) => renderer.Writer.Write( content );
}