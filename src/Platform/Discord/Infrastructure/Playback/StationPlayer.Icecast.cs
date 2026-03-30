using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.Icecast.Abstractions;
using Wadio.Platform.Api.Abstractions;
using Wadio.Platform.Sampler.Abstractions;
using Wadio.Platform.Sampler.Client.Abstractions;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class IcecastStationPlayer : StationPlayer
{
    private readonly IcecastStreamReader reader;
    private readonly IMetadataSampler sampler;

    public override event Func<Exception?, ValueTask> Ended;
    public override event Func<StationPlayerMeta, ValueTask> MetadataUpdated;

    private IcecastMetadataDictionary? metadata;

    public IcecastStationPlayer(
        IcecastStreamReader reader,
        ConcurrentDictionary<Guid, StationPlayerEntry> players,
        IMetadataSampler sampler,
        Station station ) : base( players, station )
    {
        this.reader = reader;
        this.sampler = sampler;

        reader.Ended += e => Ended?.Invoke( e ) ?? default;
        reader.MetadataRead += OnMetadataRead;
    }

    public override ValueTask<Stream> CreateAudioStream( CancellationToken cancellation = default ) => new( reader.CreateAudioStream() );

    protected override ValueTask OnDisposeAsync( ) => reader.DisposeAsync();

    private async ValueTask OnMetadataRead( IcecastMetadataDictionary metadata )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        if( MetadataUpdated is null || (this.metadata is null && metadata.Count is 0) )
        {
            // NOTE: nothing to do...
            return;
        }

        if( !metadata.Equals( this.metadata ) )
        {
            this.metadata = metadata;
            if( MetadataMapper.TryMap( metadata, out var meta ) )
            {
                await MetadataUpdated.Invoke( meta );
            }

            await sampler.Sample( new( Station.Url, Station.Id, MetadataType.Icecast )
            {
                Data = metadata,
            } );
        }
    }
}

static file class MetadataMapper
{
    private static readonly FrozenSet<string> ArtworkKeys = (new[] { "StreamArtwork", "StreamCover", "StreamThumbnail", "Artwork", "Cover", "Thumbnail", "Image" })
        .SelectMany<string, string>( key => [ key, $"{key}Url", $"{key}Uri" ] )
        .ToFrozenSet();

    private static readonly FrozenSet<string> TitleKeys = [ "Title", "StreamTitle", "StreamName", "Name" ];

    private static bool TryGetArtwork( IcecastMetadataDictionary metadata, [NotNullWhen( true )] out Uri? url )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        return TryGetUrl( metadata, ArtworkKeys, out url );

        static bool TryGetUrl( IcecastMetadataDictionary metadata, IEnumerable<string> keys, [NotNullWhen( true )] out Uri? url )
        {
            ArgumentNullException.ThrowIfNull( keys );

            if( metadata.Count is 0 )
            {
                url = default;
                return false;
            }

            foreach( var key in keys )
            {
                if( TryGetValue( metadata, key, out var value ) is true && Uri.TryCreate( value, UriKind.Absolute, out url ) )
                {
                    return true;
                }
            }

            url = default;
            return false;
        }
    }

    private static bool TryGetTitle( IcecastMetadataDictionary metadata, [NotNullWhen( true )] out string? title )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        return TryGetValue( metadata, TitleKeys, out title );
    }

    private static bool TryGetValue( IcecastMetadataDictionary metadata, string key, [NotNullWhen( true )] out string? value )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        if( metadata.Count is 0 )
        {
            value = default;
            return false;
        }

        if( metadata.TryGetValue( key, out value ) is true )
        {
            if( !string.IsNullOrEmpty( value = value?.Trim() ) )
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetValue( IcecastMetadataDictionary metadata, IEnumerable<string> keys, [NotNullWhen( true )] out string? value )
    {
        ArgumentNullException.ThrowIfNull( metadata );
        ArgumentNullException.ThrowIfNull( keys );

        if( metadata.Count is 0 )
        {
            value = default;
            return false;
        }

        foreach( var key in keys )
        {
            if( TryGetValue( metadata, key, out value ) is true )
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool TryMap( IcecastMetadataDictionary metadata, [NotNullWhen( true )] out StationPlayerMeta? meta )
    {
        ArgumentNullException.ThrowIfNull( metadata );

        if( metadata.Count is 0 )
        {
            meta = default;
            return false;
        }

        if( TryGetArtwork( metadata, out var artwork ) | TryGetTitle( metadata, out var title ) )
        {
            meta = new()
            {
                ArtworkUrl = artwork,
                Title = title,
            };

            return true;
        }

        meta = default;
        return false;
    }
}