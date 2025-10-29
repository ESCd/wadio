using System.Runtime.CompilerServices;

namespace Wadio.App.UI.Infrastructure;

internal static class Disposer
{
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static void Dispose( params IEnumerable<IDisposable> resources )
    {
        ArgumentNullException.ThrowIfNull( resources );

        foreach( var resource in resources )
        {
            resource.Dispose();
        }
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static async ValueTask DisposeAsync( ICollection<IAsyncDisposable> resources, bool clear = true )
    {
        ArgumentNullException.ThrowIfNull( resources );

        foreach( var resource in resources )
        {
            await resource.DisposeAsync();
        }

        if( clear )
        {
            resources.Clear();
        }
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static async ValueTask DisposeAsync( params IEnumerable<IAsyncDisposable> resources )
    {
        ArgumentNullException.ThrowIfNull( resources );

        foreach( var resource in resources )
        {
            await resource.DisposeAsync();
        }
    }
}