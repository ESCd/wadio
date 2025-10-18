namespace Wadio.App.Abstractions.Tests;

public sealed class WadioVersionTests
{
    [Fact]
    public void Current_DoesNotThrow( ) => Assert.NotNull( WadioVersion.Current );

    [Theory]
    [MemberData( nameof( VersionData ) )]
    public void GreaterThan_Returns_True( WadioVersion left, WadioVersion right )
    {
        if( right > left )
        {
            return;
        }

        Assert.Fail( $"{right} > {left} == {bool.FalseString}." );
    }

    [Theory]
    [MemberData( nameof( EqualVersionData ) )]
    [MemberData( nameof( VersionData ) )]
    public void GreaterThanOrEquals_Returns_True( WadioVersion left, WadioVersion right )
    {
        if( right >= left )
        {
            return;
        }

        Assert.Fail( $"{right} >= {left} == {bool.FalseString}." );
    }

    [Theory]
    [MemberData( nameof( VersionData ) )]
    public void LessThan_Returns_True( WadioVersion left, WadioVersion right )
    {
        if( left < right )
        {
            return;
        }

        Assert.Fail( $"{left} < {right} == {bool.FalseString}." );
    }

    [Theory]
    [MemberData( nameof( EqualVersionData ) )]
    [MemberData( nameof( VersionData ) )]
    public void LessThanOrEquals_Returns_True( WadioVersion left, WadioVersion right )
    {
        if( left <= right )
        {
            return;
        }

        Assert.Fail( $"{left} <= {right} == {bool.FalseString}." );
    }

    [Theory]
    [MemberData( nameof( ParseVersionData ) )]
    public void Parse_Returns_Version( string value, WadioVersion expected )
    {
        var actual = WadioVersion.Parse( value );
        Assert.Equal( expected, actual );
    }

    public static TheoryData<WadioVersion, WadioVersion> VersionData( ) => new()
    {
        { new WadioVersion( 1, 0, 0 ), new WadioVersion( 1, 1, 0 ) },
        { new WadioVersion( 1, 0, 0 ), new WadioVersion( 1, 0, 1 ) },
        { new WadioVersion( 1, 0, 0 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 1, 0, 0, 5 ), new WadioVersion( 1, 0, 0 ) },
        { new WadioVersion( 1, 0, 0, 5 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 1, 0, 0, 5 ), new WadioVersion( 1, 1, 1, 5 ) },
        { new WadioVersion( 1, 0, 0, 5 ), new WadioVersion( 1, 1, 1, 6 ) },

        { new WadioVersion( 0, 1, 0 ), new WadioVersion( 1, 0, 0 ) },
        { new WadioVersion( 0, 1, 0 ), new WadioVersion( 1, 1, 0 ) },
        { new WadioVersion( 0, 1, 0 ), new WadioVersion( 1, 0, 1 ) },
        { new WadioVersion( 0, 1, 0 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 0, 100, 0 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 0, 1, 0, 5 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 0, 1, 0, 5 ), new WadioVersion( 1, 1, 1, 5 ) },
    };

    public static TheoryData<WadioVersion, WadioVersion> EqualVersionData( ) => new()
    {
        { new WadioVersion( 1, 0, 0 ), new WadioVersion( 1, 0, 0 ) },
        { new WadioVersion( 0, 1, 0 ), new WadioVersion( 0, 1, 0 ) },
        { new WadioVersion( 0, 0, 1 ), new WadioVersion( 0, 0, 1 ) },
        { new WadioVersion( 0, 0, 0, 5 ), new WadioVersion( 0, 0, 0, 5 ) },

        { new WadioVersion( 1, 1, 0 ), new WadioVersion( 1, 1, 0 ) },
        { new WadioVersion( 1, 1, 1 ), new WadioVersion( 1, 1, 1 ) },
        { new WadioVersion( 0, 1, 1 ), new WadioVersion( 0, 1, 1 ) },
        { new WadioVersion( 1, 0, 1 ), new WadioVersion( 1, 0, 1 ) },
    };

    public static TheoryData<string, WadioVersion> ParseVersionData( ) => new()
    {
        { "1.0.0", new WadioVersion( 1, 0, 0 ) },
        { "0.1.0", new WadioVersion( 0, 1, 0 ) },
        { "0.0.1", new WadioVersion( 0, 0, 1 ) },
        { "0.0.0-rc.5", new WadioVersion( 0, 0, 0, 5 ) },
        { "0.0.0-rc.5.2", new WadioVersion( 0, 0, 0, 5.2f ) },
        { WadioVersion.Current.ToString(), WadioVersion.Current },
    };
}