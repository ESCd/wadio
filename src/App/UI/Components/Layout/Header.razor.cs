using System.Collections.Immutable;
using ESCd.Extensions.Caching.Abstractions;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Wadio.App.Abstractions;
using Wadio.App.Abstractions.Api;
using Wadio.App.UI.Infrastructure.Markdown;
using Wadio.App.UI.Interop;

namespace Wadio.App.UI.Components.Layout;

public sealed record HeaderState : State<HeaderState>
{
    public bool HasApplicationUpdated => Version is null || Version < WadioVersion.Current;
    public HistoryState? History { get; init; }
    public bool IsApplicationOutdated { get; init; }
    public ImmutableArray<Release>? Releases { get; init; }
    public WadioVersion? Version { get; init; } = !OperatingSystem.IsBrowser() ? WadioVersion.Current : default;

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

    internal static async Task<HeaderState> LoadReleases( IAsyncCache cache, IReleasesApi releases, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( cache );
        ArgumentNullException.ThrowIfNull( releases );
        ArgumentNullException.ThrowIfNull( state );

        return state with
        {
            Releases = await cache.GetOrCreateAsync(
                new( nameof( HeaderState ), nameof( Releases ) ),
                ( entry, cancellation ) => GetReleases( entry, releases, cancellation ) )
        };

        static async ValueTask<ImmutableArray<Release>> GetReleases(
            ICacheEntry entry,
            IReleasesApi releases,
            CancellationToken cancellation )
        {
            entry.SetAbsoluteExpiration( TimeSpan.FromHours( 2 ) )
                .SetSlidingExpiration( TimeSpan.FromMinutes( 15 ) );

            return [ .. await releases.Get( cancellation ).ToListAsync( cancellation ) ];
        }
    }

    internal static async Task<HeaderState> LoadVersion( LocalStorageInterop localStorage, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( localStorage );
        ArgumentNullException.ThrowIfNull( state );

        var version = await localStorage.Get<WadioVersion>( "version" );
        await localStorage.Set( "version", WadioVersion.Current );

        return state with
        {
            Version = version
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

    internal static async Task<HeaderState> RefreshOutdated( IWadioApi api, HeaderState state )
    {
        ArgumentNullException.ThrowIfNull( api );
        ArgumentNullException.ThrowIfNull( state );

        try
        {
            var version = await api.Version();
            return state with
            {
                IsApplicationOutdated = WadioVersion.Current < version,
            };
        }
        catch
        {
            return state;
        }
    }

    public sealed record HistoryState
    {
        public bool IsBackwardSupported { get; init; }
        public bool IsForwardSupported { get; init; }
    }
}

static file class HeaderCacheKeys
{
    public static readonly CacheKey Releases = new( nameof( HeaderState ), "releases" );
}

sealed file record VersionData( WadioVersion Version );

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