using System.Globalization;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Wadio.App.UI.Infrastructure.Markdown;

internal static class MentionPipelineExtensions
{
    public static MarkdownPipelineBuilder UseMentionLinks( this MarkdownPipelineBuilder pipeline, MentionOptions? options = default )
    {
        ArgumentNullException.ThrowIfNull( pipeline );

        pipeline.Extensions.ReplaceOrAdd<MentionExtension>( new MentionExtension( options ?? new() ) );
        return pipeline;
    }
}

internal sealed partial class MentionOptions
{
    public string BaseUrl { get; set; } = "/{0}";

    public string? CssClass { get; set; }

    public Regex UsernamePattern { get; set; } = DefaultUsernamePattern();

    [GeneratedRegex( @"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,38})$", RegexOptions.Compiled )]
    public static partial Regex DefaultUsernamePattern( );
}

sealed file class MentionExtension( MentionOptions? options = null ) : IMarkdownExtension
{
    private readonly MentionOptions options = options ?? new MentionOptions();

    public void Setup( MarkdownPipelineBuilder pipeline )
    {
        // Insert before link parsing so plain @name doesn’t get misinterpreted
        if( !pipeline.InlineParsers.Contains<MentionInlineParser>() )
        {
            pipeline.InlineParsers.InsertBefore<LinkInlineParser>( new MentionInlineParser( options ) );
        }
    }

    public void Setup( MarkdownPipeline pipeline, IMarkdownRenderer renderer )
    {
        if( renderer is HtmlRenderer html )
        {
            if( !html.ObjectRenderers.Contains<MentionInlineRenderer>() )
            {
                html.ObjectRenderers.InsertBefore<LinkInlineRenderer>( new MentionInlineRenderer( options ) );
            }
        }
    }
}

sealed file class MentionInline( string username, string href ) : LeafInline
{
    public string Username { get; } = username ?? throw new ArgumentNullException( nameof( username ) );
    public string Href { get; } = href ?? throw new ArgumentNullException( nameof( href ) );
}

sealed file class MentionInlineParser : InlineParser
{
    private readonly MentionOptions options;

    public MentionInlineParser( MentionOptions options )
    {
        this.options = options ?? throw new ArgumentNullException( nameof( options ) );
        OpeningCharacters = [ '@' ];
    }

    public override bool Match( InlineProcessor processor, ref StringSlice slice )
    {
        // We’re currently on '@'
        var start = slice.Start;
        var current = slice.CurrentChar; // '@'
        if( current is not '@' )
        {
            return false;
        }

        // Reject '@@' (often used for escaping)
        var peek = slice.PeekChar();
        if( peek is '@' )
        {
            return false;
        }

        // Must be at a reasonable boundary: start-of-line, whitespace, or punctuation.
        // (Prevents matching email addresses like name@example.com)
        var prevChar = slice.PeekCharExtra( -1 );
        if( prevChar != '\0' && !IsBoundary( prevChar ) ) return false;

        // Move past '@' and capture username chars
        slice.NextChar();
        var userStart = slice.Start;

        // Allowed username characters (looser scan); we’ll validate with Regex at the end.
        while( true )
        {
            var character = slice.CurrentChar;
            if( character is '\0' )
            {
                break;
            }

            if( !IsUsernameChar( character ) )
            {
                break;
            }

            slice.NextChar();
        }

        var userEnd = slice.Start - 1;
        if( userEnd < userStart ) { slice.Start = start; return false; }

        var username = slice.Text.Substring( userStart, userEnd - userStart + 1 );

        // Extra guard: reject if the next char looks like part of an email (i.e., immediately a domain separator)
        var after = slice.CurrentChar;
        if( after is '.' && !IsBoundary( slice.PeekChar() ) )
        {
            slice.Start = start;
            return false;
        }

        // Validate against Regex (length/charset)
        if( !options.UsernamePattern.IsMatch( username ) )
        {
            slice.Start = start;
            return false;
        }

        var href = BuildHref( options.BaseUrl, username );

        // Build inline node and push it
        var mention = new MentionInline( username, href )
        {
            Span = new SourceSpan( start, slice.Start - 1 ),
            Line = processor.GetSourcePosition( start, out var _, out var column ),
            Column = column,
        };

        processor.Inline = mention;
        return true;
    }

    private static string BuildHref( string baseUrl, string username ) => baseUrl?.Contains( "{0}" ) is true
        ? string.Format( CultureInfo.InvariantCulture, baseUrl, Uri.EscapeDataString( username ) )
        : (baseUrl ?? string.Empty) + Uri.EscapeDataString( username );

    private static bool IsBoundary( char value ) => char.IsWhiteSpace( value ) || value is '\0' || char.IsPunctuation( value );
    private static bool IsUsernameChar( char value ) => char.IsLetterOrDigit( value ) || value is '_' || value is '.' || value is '-';
}

sealed file class MentionInlineRenderer( MentionOptions options ) : HtmlObjectRenderer<MentionInline>
{
    private readonly MentionOptions options = options ?? throw new ArgumentNullException( nameof( options ) );

    protected override void Write( HtmlRenderer renderer, MentionInline obj )
    {
        if( RendererDisableEscape( renderer ) )
        {
            return;
        }

        renderer.Write( "<a href=\"" ).WriteEscape( obj.Href ).Write( "\"" );
        if( !string.IsNullOrWhiteSpace( options.CssClass ) )
        {
            renderer.Write( " class=\"" ).WriteEscape( options.CssClass ).Write( "\"" );
        }

        // Safe defaults for user content links
        renderer.Write( "target=\"_blank\" rel=\"nofollow noopener\"" )
            .Write( ">" )
            .Write( '@' )
            .WriteEscape( obj.Username )
            .Write( "</a>" );
    }

    // Guard: if renderer is writing plain text, don’t inject tags
    private static bool RendererDisableEscape( HtmlRenderer renderer ) => renderer is null || renderer.EnableHtmlForInline is false;
}