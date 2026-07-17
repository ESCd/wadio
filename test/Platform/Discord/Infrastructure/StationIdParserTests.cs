namespace Wadio.Platform.Discord.Infrastructure.Tests;

public class StationIdParserTests
{
    [Theory]
    [InlineData( "https://wadio.live/station/6e2e12b2-56a8-405f-8b59-6e9dcac3e11c", "6e2e12b2-56a8-405f-8b59-6e9dcac3e11c" )]
    [InlineData( "https://localhost:1337/station/6e2e12b2-56a8-405f-8b59-6e9dcac3e11c", "6e2e12b2-56a8-405f-8b59-6e9dcac3e11c" )]
    [InlineData( "6e2e12b2-56a8-405f-8b59-6e9dcac3e11c", "6e2e12b2-56a8-405f-8b59-6e9dcac3e11c" )]
    [InlineData( "asdasdasd", default )]
    [InlineData( "https://localhost:1337/station/123", default )]
    [InlineData( "https://localhost:1337/stations/123", default )]
    [InlineData( "https://localhost:1337/about", default )]
    public void TryParse_Should_ParseId( string value, Guid expected )
    {
        var parsed = StationIdParser.TryParse( value, out var result );
        if( expected != default )
        {
            Assert.True( parsed );
            Assert.Equal( expected, result );

            return;
        }

        Assert.False( parsed );
    }
}