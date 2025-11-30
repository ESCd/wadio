using Microsoft.Playwright;

namespace ScreenCap;

internal sealed class CaptureContext : IAsyncDisposable
{
    private readonly IBrowserContext context;
    private readonly CaptureDevice device;

    private CaptureContext( IBrowserContext context, CaptureDevice device )
    {
        this.context = context;
        this.device = device;
    }

    public static async Task<CaptureContext> Create( IPlaywright playwright, IBrowser browser, CaptureDevice device = CaptureDevice.Desktop )
    {
        ArgumentNullException.ThrowIfNull( playwright );
        ArgumentNullException.ThrowIfNull( browser );

        return new( await browser.NewContextAsync( new( device is CaptureDevice.Mobile ? playwright.Devices[ "iPhone 15 Plus" ] : default! )
        {
            BaseURL = "http://localhost:5001",
            Geolocation = new()
            {
                Latitude = 40.7128f,
                Longitude = -74.0060f,
            },
            IgnoreHTTPSErrors = true,
            IsMobile = device is CaptureDevice.Mobile,
            Permissions = [ "geolocation" ],
        } ), device );
    }

    public ValueTask DisposeAsync( ) => context.DisposeAsync();

    public async Task CaptureAsync( CaptureOptions options )
    {
        ArgumentNullException.ThrowIfNull( options );

        var page = await context.NewPageAsync();

        await page.GotoAsync( options.Path );
        if( options.Action is not null )
        {
            await options.Action( page );
        }

        _ = await page.ScreenshotAsync( new()
        {
            Path = $"./screens/{device}/{options.Name}.png",
            FullPage = false,
        } );

        await page.CloseAsync();
    }
}

internal enum CaptureDevice
{
    Desktop,
    Mobile,
}

internal sealed record CaptureOptions( string Name, string Path )
{
    public Func<IPage, Task>? Action { get; init; }
}