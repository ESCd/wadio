using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Wadio.Extensions.Icecast;
using Wadio.Extensions.Icecast.Abstractions;
using Wadio.Extensions.RadioBrowser;
using Wadio.Extensions.RadioBrowser.Abstractions;

await using var services = ScraperServices.Create();

var results = new List<ScraperResult>();
await foreach( var station in GetRandomStations( services.RadioBrowser, 25 ) )
{
    Console.WriteLine( $"scraping station {station.Id}@{station.Url}..." );

    var result = await ScrapeMetadata( services.Icecast, station );
    results.Add( result );

    Console.WriteLine( $"[{(result.IsSuccess ? "+" : "!")}] scraped station" );
    Console.WriteLine();
}

await using( var file = File.OpenWrite( Path.Combine( Path.Join( Directory.GetCurrentDirectory(), "results" ), $"{DateTime.Now.Ticks}.json" ) ) )
{
    await JsonSerializer.SerializeAsync(
        file,
        results,
        ScraperJsonContext.Default.ListScraperResult );
}

Console.WriteLine( "Wrote results file!" );

static async IAsyncEnumerable<Station> GetRandomStations( IRadioBrowserClient radioBrowser, int count = 5 )
{
    ArgumentNullException.ThrowIfNull( radioBrowser );
    ArgumentOutOfRangeException.ThrowIfZero( count );

    var statistics = await radioBrowser.GetStatistics();
    ArgumentNullException.ThrowIfNull( statistics );

    var stations = new HashSet<Guid>( count );
    while( stations.Count < count )
    {
        await foreach( var station in GetRandom( ( uint )(count - stations.Count) ) )
        {
            if( stations.Add( station.Id ) )
            {
                yield return station;
            }
        }
    }

    IAsyncEnumerable<Station> GetRandom( uint count ) => radioBrowser.Search( new()
    {
        HideBroken = true,
        IsHttps = true,
        Limit = count,
        Offset = ( uint )Random.Shared.Next( 0, ( int )(statistics.Stations - statistics.BrokenStations - count) ),
        Order = StationOrderBy.Random,
    } ).Where( station => !station.IsHls );
}

static async ValueTask<ScraperResult> ScrapeMetadata( IcecastMetadataClient icecast, Station station )
{
    ArgumentNullException.ThrowIfNull( icecast );
    ArgumentNullException.ThrowIfNull( station );

    if( station.IsHls )
    {
        throw new ArgumentException( "Hls stations are not supported." );
    }

    try
    {
        await using var reader = await icecast.GetReader( station.Url );

        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter( TimeSpan.FromMinutes( 2.5 ) );

        return ScraperResult.From( station ) with
        {
            Metadata = await reader.WaitUntilMetadata( cancellation.Token )
        };
    }
    catch( Exception e )
    {
        return ScraperResult.From( station ) with
        {
            ErrorMessage = $"{e}",
            StatusCode = (e as HttpRequestException)?.StatusCode,
        };
    }
}

internal sealed record ScraperResult( Guid StationId, Uri StationUrl )
{
    public string? ErrorMessage { get; init; }
    public bool IsSuccess => string.IsNullOrEmpty( ErrorMessage ) && Metadata is not null;
    public IcecastMetadataDictionary? Metadata { get; init; }
    public HttpStatusCode? StatusCode { get; init; }

    public static ScraperResult From( Station station )
    {
        ArgumentNullException.ThrowIfNull( station );
        return new( station.Id, station.Url );
    }
}

sealed file class ScraperServices( ServiceProvider services ) : IAsyncDisposable
{
    public IcecastMetadataClient Icecast { get; } = services.GetRequiredService<IcecastMetadataClient>();
    public IRadioBrowserClient RadioBrowser { get; } = services.GetRequiredService<IRadioBrowserClient>();

    public static ScraperServices Create( )
    {
        var services = new ServiceCollection()
            .AddIcecastClient()
            .AddRadioBrowser( builder => builder.UsePingHostResolver() )
            .BuildServiceProvider();

        return new( services );
    }

    public ValueTask DisposeAsync( ) => services.DisposeAsync();
}

[JsonSerializable( typeof( List<ScraperResult> ) )]
[JsonSourceGenerationOptions( DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true )]
internal sealed partial class ScraperJsonContext : JsonSerializerContext;