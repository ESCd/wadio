using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using ESCd.Extensions.Caching.Abstractions;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Infrastructure.Markdown;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components.Layout;

public sealed record HeaderState : State<HeaderState>
{
    private const uint StationCount = 4;

    public HistoryState? History { get; init; }
    public bool IsLoading { get; init; }
    public ImmutableArray<Release>? Releases { get; init; }
    public ImmutableArray<Station>? Stations { get; init; }

    internal static async Task<HeaderState> LoadHistorySupport( DOMInterop dom, HistoryInterop history, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( dom );
        ArgumentNullException.ThrowIfNull( history );
        ArgumentNullException.ThrowIfNull( state );

        if( !await history.IsNavigationApiSupported() )
        {
            return state;
        }

        if( await dom.IsApplicationInstalled() || await dom.IsFullscreen() )
        {
            return await RefreshHistorySupport( history, state );
        }

        return state with
        {
            History = default,
        };
    }

    internal static async Task<HeaderState> RefreshHistorySupport( HistoryInterop history, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( history );
        ArgumentNullException.ThrowIfNull( state );

        var (backward, forward) = await history.CanNavigate();
        return state with
        {
            History = new()
            {
                IsBackwardSupported = backward,
                IsForwardSupported = forward,
            }
        };
    }

    internal static async Task<HeaderState> LoadReleases( IAsyncCache cache, IGitHubClient github, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( cache );
        ArgumentNullException.ThrowIfNull( github );
        ArgumentNullException.ThrowIfNull( state );

        return state with
        {
            Releases = await cache.GetOrCreateAsync(
                new( nameof( HeaderState ), nameof( Releases ) ),
                ( entry, cancellation ) => GetReleases( entry, github, cancellation ) )
        };

        static async ValueTask<ImmutableArray<Release>> GetReleases(
            ICacheEntry entry,
            IGitHubClient github,
            CancellationToken cancellation )
        {
            entry.SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 15 ) );

            return [ .. await github.Repository.Release.GetAll( "ESCd", "wadio" ) ];
        }
    }

    internal static HeaderState Reset( HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( state );
        return state with
        {
            IsLoading = false,
            Stations = default,
        };
    }

    internal static async IAsyncEnumerable<HeaderState> Search( IStationsApi api, string query, HeaderState state, [EnumeratorCancellation] CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentException.ThrowIfNullOrWhiteSpace( query );
        ArgumentNullException.ThrowIfNull( state );

        yield return state = (state with
        {
            IsLoading = true,
            Stations = [],
        });

        yield return await Search( api, query, state, cancellation );

        static async Task<HeaderState> Search( IStationsApi api, string query, HeaderState state, CancellationToken cancellation )
        {
            ArgumentNullException.ThrowIfNull( api );
            ArgumentException.ThrowIfNullOrWhiteSpace( query );
            ArgumentNullException.ThrowIfNull( state );

            var search = api.Search( new()
            {
                Count = StationCount,
                Name = query,
                Order = StationOrderBy.Name,
            }, cancellation );

            return state with
            {
                IsLoading = false,
                Stations = [ .. await search.ToListAsync( cancellation ) ],
            };
        }
    }

    public sealed record HistoryState
    {
        public bool IsBackwardSupported { get; init; }
        public bool IsForwardSupported { get; init; }
    }
}

internal static class ReleaseNotesDefaults
{
    public static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .UseGitHubLinks( new()
        {
            Repositories = [ "ESCd/wadio" ]
        } )
        .UseMentionLinks( new()
        {
            BaseUrl = "https://github.com/"
        } )
        .Use( new ShiftHeadingExtension( 1 ) )
        .Use( new ExternalLinksExtension() )
        .UseReferralLinks( "noopener", "ugc" )
        .Build();
}