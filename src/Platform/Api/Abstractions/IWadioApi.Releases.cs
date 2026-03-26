using Wadio.Platform.Abstractions;

namespace Wadio.Platform.Api.Abstractions;

public interface IReleasesApi
{
    public IAsyncEnumerable<Release> Get( CancellationToken cancellation = default );
}

public sealed record Release( bool IsLatest, string Notes, DateTimeOffset PublishedAt, Uri Url, WadioVersion Version );