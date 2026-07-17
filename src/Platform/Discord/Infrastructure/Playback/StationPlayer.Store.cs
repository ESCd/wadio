using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Wadio.Platform.Discord.Infrastructure.Playback;

internal sealed class StationPlayerStore : IAsyncDisposable
{
    private readonly ConcurrentDictionary<ulong, StationPlayerController> controllers = new();

    public int Count => controllers.Count;

    public async ValueTask<bool> RemoveAsync( ulong guildId )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );

        if( controllers.TryRemove( guildId, out var controller ) )
        {
            await controller.DisposeAsync();
            return true;
        }

        return false;
    }

    public bool TryGetValue( ulong guildId, [NotNullWhen( true )] out StationPlayerController? controller ) => controllers.TryGetValue( guildId, out controller );

    public async ValueTask<bool?> TryRemoveAsync( ulong guildId, StationPlayerController controller )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );

        if( controllers.TryRemove( guildId, out var existing ) )
        {
            await existing.DisposeAsync();
            return controller == existing;
        }

        return default;
    }

    public async ValueTask<StationPlayerController> Update( ulong guildId, StationPlayerController controller )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero( guildId );
        ArgumentNullException.ThrowIfNull( controller );

        if( controllers.TryRemove( guildId, out var existing ) )
        {
            await existing.DisposeAsync();
        }

        controllers[ guildId ] = controller;
        return controller;
    }

    public ValueTask DisposeAsync( ) => Disposer.DisposeAsync( controllers );
}