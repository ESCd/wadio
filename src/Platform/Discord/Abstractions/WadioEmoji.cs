using NetCord;

namespace Wadio.Platform.Discord.Abstractions;

public readonly struct WadioEmoji( string name, ulong id )
{
    private readonly ulong id = id;

    public static readonly WadioEmoji ActionKey = new( "action_key", 1485443790293696606 );
    public static readonly WadioEmoji ArrowBack = new( "arrow_back", 1486055217849040928 );
    public static readonly WadioEmoji ArrowForward = new( "arrow_forward", 1486055245178994859 );
    public static readonly WadioEmoji Adb = new( "adb", 1485177606264590416 );
    public static readonly WadioEmoji ExpandCircle = new( "expand_circle", 1484825579474915378 );
    public static readonly WadioEmoji Hls = new( "hls", 1485567218186981406 );
    public static readonly WadioEmoji HlsOff = new( "hls_off", 1485567189237895298 );
    public static readonly WadioEmoji Language = new( "language", 1485567245798211594 );
    public static readonly WadioEmoji LocationChip = new( "location_chip", 1485532544001638530 );
    public static readonly WadioEmoji MonkeyAtPeace = new( "monkey_at_peace", 1487691535981219880 );
    public static readonly WadioEmoji MusicCast = new( "music_cast", 1485555463759200339 );
    public static readonly WadioEmoji PlayCircle = new( "play_circle", 1484825677646790759 );
    public static readonly WadioEmoji PlayDisabled = new( "play_disabled", 1484825641521123439 );
    public static readonly WadioEmoji StopCircle = new( "stop_circle", 1484825657476382810 );
    public static readonly WadioEmoji Tag = new( "tag", 1485549413874139146 );
    public static readonly WadioEmoji ThumbsUp = new( "thumbs_up", 1484825761650049114 );
    public static readonly WadioEmoji VotingChip = new( "voting_chip", 1485532513748389989 );

    public override string ToString( ) => $"<:{name}:{id}>";

    public static implicit operator EmojiProperties( WadioEmoji emoji ) => EmojiProperties.Custom( emoji.id );
    public static implicit operator ulong( WadioEmoji emoji ) => emoji.id;
}