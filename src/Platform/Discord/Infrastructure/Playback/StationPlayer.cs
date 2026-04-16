using System.Collections.Concurrent;
using Wadio.Platform.Api.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal abstract class StationPlayer( ConcurrentDictionary<Guid, StationPlayerEntry> players, Station station ) : IAsyncDisposable
{
    public Station Station => station;
    public Codec Codec => station.Codec;

    public abstract event Func<Exception?, ValueTask> Ended;
    public abstract event Func<StationPlayerMeta, ValueTask> MetadataUpdated;

    public abstract ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default );

    public async ValueTask DisposeAsync( )
    {
        if( players.TryGetValue( station.Id, out var entry ) )
        {
            var updated = entry with
            {
                Count = Math.Max( 0, entry.Count - 1 )
            };

            if( players.TryUpdate( station.Id, updated, entry ) )
            {
                if( updated.Count is 0 && players.Remove( station.Id, out var removed ) )
                {
                    await removed.Value.OnDisposeAsync();
                    if( removed.Value != this )
                    {
                        await OnDisposeAsync();
                    }
                }
            }
        }
    }

    protected virtual ValueTask OnDisposeAsync( ) => default;
}

internal sealed record StationPlayerMeta
{
    public static readonly StationPlayerMeta Empty = new();
    public static readonly StationPlayerMeta Loading = new()
    {
        Title = "Loading..."
    };

    public Uri? ArtworkUrl { get; init; }
    public string? Title { get; init; }
}

internal static class StationPlayerExtensions
{
    public static async Task<StationPlayerMeta> WaitUntilMetadata( this StationPlayer player, CancellationToken cancellation = default )
    {
        ArgumentNullException.ThrowIfNull( player );

        var completion = new TaskCompletionSource<StationPlayerMeta>( TaskCreationOptions.RunContinuationsAsynchronously );
        try
        {
            await using( cancellation.Register( OnCancelled ).ConfigureAwait( false ) )
            {
                player.Ended += OnEnded;
                player.MetadataUpdated += OnMetadata;
                return await completion.Task.ConfigureAwait( false );
            }
        }
        finally
        {
            player.MetadataUpdated -= OnMetadata;
            player.Ended -= OnEnded;
        }

        void OnCancelled( ) => completion.TrySetCanceled( cancellation );
        ValueTask OnEnded( Exception? exception )
        {
            completion.TrySetException( exception ?? new EndOfStreamException( "The player has ended." ) );
            return default;
        }

        ValueTask OnMetadata( StationPlayerMeta meta )
        {
            completion.SetResult( meta );
            return ValueTask.CompletedTask;
        }
    }

    public static async Task WaitUntilMetadata( this StationPlayer player, TimeSpan timeout, CancellationToken cancellation )
    {
        ArgumentNullException.ThrowIfNull( player );

        using var token = new CancellationTokenSource( timeout );
        using var combined = CancellationTokenSource.CreateLinkedTokenSource( token.Token, cancellation );

        try
        {
            _ = await player.WaitUntilMetadata( combined.Token );
        }
        catch( OperationCanceledException e ) when( e.CancellationToken == combined.Token && token.IsCancellationRequested )
        {
            throw new TimeoutException( $"Timed out after {timeout} while waiting for metadata.", e );
        }
    }
}