namespace Wadio.Extensions.RadioBrowser.Abstractions;

public interface IRadioBrowserClient
{
    public IAsyncEnumerable<Country> GetCounties( GetCountriesParameters parameters, CancellationToken cancellation = default );
    public IAsyncEnumerable<Language> GetLanguages( GetLanguagesParameters parameters, CancellationToken cancellation = default );
    public ValueTask<Station?> GetStation( Guid stationId, CancellationToken cancellation = default );
    public Task<ServiceStatistics?> GetStatistics( CancellationToken cancellation = default );
    public IAsyncEnumerable<Tag> GetTags( GetTagsParameters parameters, CancellationToken cancellation = default );
    public IAsyncEnumerable<Station> Search( SearchParameters parameters, CancellationToken cancellation = default );
}