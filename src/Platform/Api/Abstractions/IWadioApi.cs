using Wadio.Platform.Abstractions;

namespace Wadio.Platform.Api.Abstractions;

public interface IWadioApi
{
    public ICountriesApi Countries { get; }
    public ILanguagesApi Languages { get; }
    public IReleasesApi Releases { get; }
    public IStationsApi Stations { get; }
    public ITagsApi Tags { get; }

    public ValueTask<WadioVersion> Version( CancellationToken cancellation = default );
}