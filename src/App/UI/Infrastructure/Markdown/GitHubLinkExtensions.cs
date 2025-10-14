using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Wadio.App.UI.Infrastructure.Markdown;

internal static partial class GitHubPipelineExtensions
{
    public static MarkdownPipelineBuilder UseGitHubLinks( this MarkdownPipelineBuilder pipeline, GitHubLinkOptions? options = default )
    {
        ArgumentNullException.ThrowIfNull( pipeline );

        pipeline.Extensions.ReplaceOrAdd<GitHubLinkExtension>( new GitHubLinkExtension( options ?? new() ) );
        return pipeline;
    }
}

internal sealed class GitHubLinkOptions
{
    public List<string> Repositories { get; set; } = [];
}

sealed file class GitHubLinkExtension( GitHubLinkOptions options ) : IMarkdownExtension
{
    private readonly IReadOnlyList<string> repositories = [ .. options.Repositories.Select( repository => repository.TrimEnd( '/' ) + '/' ) ];

    public void Setup( MarkdownPipelineBuilder pipeline )
    {
    }

    public void Setup( MarkdownPipeline pipeline, IMarkdownRenderer renderer )
    {
        if( renderer is HtmlRenderer htmlRenderer )
        {
            var link = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>()!;
            if( link is not null )
            {
                link.TryWriters.Remove( TryRenderLink );
                link.TryWriters.Add( TryRenderLink );
            }
        }
    }

    private bool TryRenderLink( HtmlRenderer renderer, LinkInline link )
    {
        ArgumentNullException.ThrowIfNull( renderer );
        ArgumentNullException.ThrowIfNull( link );

        foreach( var repository in repositories )
        {
            var url = new Uri( $"https://github.com/{repository}" );
            if( link.Url?.StartsWith( new Uri( url, "compare/" ).ToString(), StringComparison.OrdinalIgnoreCase ) is true
                || link.Url?.StartsWith( new Uri( url, "issues/" ).ToString(), StringComparison.OrdinalIgnoreCase ) is true
                || link.Url?.StartsWith( new Uri( url, "pull/" ).ToString(), StringComparison.OrdinalIgnoreCase ) is true )
            {
                SetLinkText( link );
                return false;
            }

            return false;
        }

        return false;

        static void SetLinkText( LinkInline link )
        {
            var url = new Uri( link.Url! );
            link.Label = url.Segments[ ^2 ].TrimEnd( '/' ) switch
            {
                "compare" => url.Segments[ ^1 ].TrimEnd( '/' ),
                "issues" or "pull" => $"#{url.Segments[ ^1 ].TrimEnd( '/' )}",
                _ => link.Label,
            };

            link.Title = url.Segments[ ^2 ].TrimEnd( '/' ) switch
            {
                "compare" => $"Compare {link.Label} on GitHub",
                "issues" => $"Issue {link.Label} on GitHub",
                "pull" => $"Pull Request {link.Label} on GitHub",
                _ => link.Title,
            };

            if( !string.IsNullOrEmpty( link.Label ) )
            {
                link.Clear();
                link.AppendChild( new LiteralInline( link.Label ) );
            }
        }
    }
}

