namespace Wadio.Platform.Api.Abstractions;

public interface ILanguagesApi
{
    public IAsyncEnumerable<Language> Get( CancellationToken cancellation = default );
}

public sealed record Language( string Code, uint Count, string Name );