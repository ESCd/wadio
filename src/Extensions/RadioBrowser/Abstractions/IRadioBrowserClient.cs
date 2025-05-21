namespace Wadio.Extensions.RadioBrowser.Abstractions;

public interface IRadioBrowserClient
{
    public ValueTask<Station?> GetStation( Guid stationId, CancellationToken cancellation = default );
    public Task<ServiceStatistics?> GetStatistics( CancellationToken cancellation = default );
    public IAsyncEnumerable<Station> Search( SearchParameters parameters, CancellationToken cancellation = default );
}