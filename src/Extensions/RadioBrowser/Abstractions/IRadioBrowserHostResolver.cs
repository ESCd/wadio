namespace Wadio.Extensions.RadioBrowser.Abstractions;

public interface IRadioBrowserHostResolver
{
    public ValueTask<RadioBrowserHost> Resolve( CancellationToken cancellation = default );
}