using System.Diagnostics;

namespace Wadio.Extensions.Icecast.Tests;

public sealed class IcecastMetadataProviderTests
{
    [Fact]
    public async Task Reader_Throws_TaskCancelledException( )
    {
        using var client = new HttpClient();

        // await using var reader = await client.GetIcecastReader( new( "https://media-ssl.musicradio.com/Heart80sMP3" ) );
        await using var reader = await client.GetIcecastReader( new( "https://liveradio.swr.de/sw282p3/swr3/play.mp3" ) );

        // await using var reader = await client.GetIcecastReader( new( "https://audio.jurnalfm.md/hq" ) );

        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter( TimeSpan.FromSeconds( 5 ) );

        await Assert.ThrowsAsync<InvalidDataException>( async ( ) =>
        {
            await foreach( var metadata in reader.AsAsyncEnumerable( cancellation.Token ) )
            {
                Assert.NotEmpty( metadata );
                Debug.WriteLine( metadata.StreamTitle );
            }
        } );
    }
}