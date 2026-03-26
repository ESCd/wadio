namespace Wadio.Platform.Api.Abstractions;

public interface ITagsApi
{
    public IAsyncEnumerable<Tag> Get( CancellationToken cancellation = default );
}

public sealed record Tag( uint Count, string Name );