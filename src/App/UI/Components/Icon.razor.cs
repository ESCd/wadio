namespace Wadio.App.UI.Components;

public readonly struct IconName( string name ) : IEquatable<IconName>
{
    private readonly string name = name;

    public static readonly IconName BugReport = new( "bug_report" );
    public static readonly IconName Cached = new( "cached" );
    public static readonly IconName Casino = new( "casino" );
    public static readonly IconName CopyAll = new( "copy_all" );
    public static readonly IconName Explore = new( "explore" );
    public static readonly IconName Help = new( "help" );
    public static readonly IconName Hls = new( "hls" );
    public static readonly IconName Info = new( "info" );
    public static readonly IconName KeyboardArrowDown = new( "keyboard_arrow_down" );
    public static readonly IconName LocationChip = new( "location_chip" );
    public static readonly IconName Menu = new( "menu" );
    public static readonly IconName MenuOpen = new( "menu_open" );
    public static readonly IconName OpenInNew = new( "open_in_new" );
    public static readonly IconName PlayArrow = new( "play_arrow" );
    public static readonly IconName Radio = new( "radio" );
    public static readonly IconName Stop = new( "stop" );
    public static readonly IconName VolumeDown = new( "volume_down" );
    public static readonly IconName VolumeMute = new( "volume_mute" );
    public static readonly IconName VolumeOff = new( "volume_off" );
    public static readonly IconName VolumeUp = new( "volume_up" );
    public static readonly IconName VotingChip = new( "voting_chip" );
    public static readonly IconName WebTraffic = new( "web_traffic" );

    /// <inheritdoc/>
    public bool Equals( IconName other ) => name.Equals( other.name, StringComparison.Ordinal );

    /// <inheritdoc/>
    public override bool Equals( object? obj ) => obj is IconName icon && Equals( icon );

    /// <inheritdoc/>
    public override string ToString( ) => name;

    public static bool operator ==( IconName left, IconName right ) => left.Equals( right );
    public static bool operator !=( IconName left, IconName right ) => !(left == right);

    public override int GetHashCode( ) => name.GetHashCode();
}