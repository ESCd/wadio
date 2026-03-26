namespace Wadio.Platform.Api.Abstractions;

public interface ICountriesApi
{
    public IAsyncEnumerable<Country> Get( CancellationToken cancellation = default );
}

public sealed record Country( string Code, uint Count, string Name );