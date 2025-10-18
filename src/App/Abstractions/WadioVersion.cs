using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Wadio.App.Abstractions;

/// <summary> Represents the version number for a specific build of the application. </summary>
[ImmutableObject( true )]
public sealed record class WadioVersion : IComparable<WadioVersion>
{
    /// <summary> A reference to the current version of the application. </summary>
    public static readonly WadioVersion Current = FromAssembly( typeof( WadioVersion ).Assembly );

    /// <summary> The major version. </summary>
    public int Major { get; }

    /// <summary> The minor version. </summary>
    public int Minor { get; }

    /// <summary> The patch, or hotfix, version. </summary>
    public int Patch { get; }

    /// <summary> The height of a pre-release in relation to the version. </summary>
    public float? Candidate { get; }

    /// <summary> Additional versioning metadata for tracking. </summary>
    public string? Metadata { get; }

    public WadioVersion( int major, int minor, int patch, float? candidate = null, string? metadata = null )
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Candidate = candidate;
        Metadata = metadata;
    }

    /// <inheritdoc/>
    public int CompareTo( WadioVersion? other )
    {
        if( other is null )
        {
            return 1;
        }

        var major = Major.CompareTo( other.Major );
        if( major is not 0 )
        {
            return major;
        }

        var minor = Minor.CompareTo( other.Minor );
        if( minor is not 0 )
        {
            return minor;
        }

        var path = Patch.CompareTo( other.Patch );
        if( path is not 0 )
        {
            return path;
        }

        return Nullable.Compare( Candidate, other.Candidate );
    }

    public static WadioVersion FromAssembly( Assembly assembly )
    {
        ArgumentNullException.ThrowIfNull( assembly );

        float? candidate = default;
        string? metadata = default;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if( !string.IsNullOrEmpty( informational ) )
        {
            var index = informational.IndexOf( '-' );
            if( index > 0 )
            {
                var end = informational.IndexOf( '+' );
                if( end < 0 )
                {
                    end = informational.Length;
                }

                index += 4;
                candidate = float.Parse( informational[ index..end ], CultureInfo.InvariantCulture );
            }

            index = informational.IndexOf( '+' );
            if( index > 0 )
            {
                metadata = informational[ (index + 1)..(index + 8) ];
            }
        }

        var version = assembly.GetName().Version!;
        return new( version.Major, version.Minor, version.Build, candidate, metadata );
    }

    public static WadioVersion Parse( string version )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( version );

        var major = 0;
        var minor = 0;
        var patch = 0;
        float? candidate = default;
        string? metadata = default;

        var main = version;
        var index = version.IndexOf( '+' );
        if( index > 0 )
        {
            metadata = version[ (index + 1).. ];
            main = version[ ..index ];
        }

        index = main.IndexOf( '-' );
        if( index > 0 )
        {
            var end = main.IndexOf( '+' );
            if( end < 0 )
            {
                end = main.Length;
            }

            index += 4;
            candidate = float.Parse( main[ index..end ], CultureInfo.InvariantCulture );
            main = main[ ..(index - 4) ];
        }

        var segments = main.Split( '.' );
        if( segments.Length > 0 )
        {
            major = int.Parse( segments[ 0 ], CultureInfo.InvariantCulture );
        }

        if( segments.Length > 1 )
        {
            minor = int.Parse( segments[ 1 ], CultureInfo.InvariantCulture );
        }

        if( segments.Length > 2 )
        {
            patch = int.Parse( segments[ 2 ], CultureInfo.InvariantCulture );
        }

        return new( major, minor, patch, candidate, metadata );
    }

    /// <inheritdoc/>
    public override string ToString( )
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if( Candidate.HasValue )
        {
            version += $"-rc.{Candidate:#.##}";
        }

        if( !string.IsNullOrEmpty( Metadata ) )
        {
            version += $"+{Metadata}";
        }

        return version;
    }

    /// <inheritdoc/>
    public static implicit operator string( WadioVersion version ) => version.ToString();

    /// <inheritdoc/>
    public static bool operator <( WadioVersion left, WadioVersion right ) => left.CompareTo( right ) < 0;

    /// <inheritdoc/>
    public static bool operator >( WadioVersion left, WadioVersion right ) => left.CompareTo( right ) > 0;

    /// <inheritdoc/>
    public static bool operator <=( WadioVersion left, WadioVersion right ) => left.CompareTo( right ) <= 0;

    /// <inheritdoc/>
    public static bool operator >=( WadioVersion left, WadioVersion right ) => left.CompareTo( right ) >= 0;
}