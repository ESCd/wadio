namespace Wadio.App.UI.Infrastructure.Imports;

internal static class PlatformGuard
{
    public static void ThrowIfNotBrowser( string? message = default )
    {
        if( !OperatingSystem.IsBrowser() )
        {
            throw new PlatformNotSupportedException( message );
        }
    }
}