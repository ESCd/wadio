using Microsoft.Playwright;
using ScreenCap;

if( Microsoft.Playwright.Program.Main( [ "install", "--with-deps", "chromium" ] ) is not 0 )
{
    throw new InvalidProgramException( $"Playwright failed to install browsers and dependencies." );
}

await using var factory = await StartServer();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync( new()
{
    Headless = true,
    Timeout = 150_000,
} );

await using var desktop = await CaptureContext.Create(
    playwright,
    browser,
    CaptureDevice.Desktop );

await using var mobile = await CaptureContext.Create(
    playwright,
    browser,
    CaptureDevice.Mobile );

var captures = new CaptureOptions[]
{
    new("About", "/")
    {
        Action = OnWaitForAbout
    },
    new("Discover", "/"),
    new("Explore", "/explore")
    {
        Action = _ => Task.Delay( TimeSpan.FromSeconds( 5 ))
    },
    new("Now Playing", "/station/08367f8c-6b57-4d59-a8b2-c0c7a933c5b5")
    {
        Action = OnWaitForPlayer
    },
    new("Search", "/search?Order=1")
    {
        Action = _ => Task.Delay( TimeSpan.FromSeconds( 2.5 ))
    },
    new("Station", "/station/08367f8c-6b57-4d59-a8b2-c0c7a933c5b5")
    {
        Action = _ => Task.Delay(TimeSpan.FromSeconds(2.5))
    },
};

foreach( var capture in captures )
{
    await desktop.CaptureAsync( capture );
    await mobile.CaptureAsync( capture );
}

static async Task OnWaitForAbout( IPage page )
{
    ArgumentNullException.ThrowIfNull( page );

    var locator = page.Locator( "button[rel='help']" );
    await locator.WaitForAsync( new()
    {
        State = WaitForSelectorState.Visible
    } );

    await locator.ClickAsync();
    await Task.Delay( TimeSpan.FromSeconds( 2.5 ) );
}

static async Task OnWaitForPlayer( IPage page )
{
    ArgumentNullException.ThrowIfNull( page );

    var locator = page.Locator( "main>div>div>div[role='button'][title='Play']" );
    await locator.WaitForAsync( new()
    {
        State = WaitForSelectorState.Visible,
    } );

    await locator.ClickAsync();
    await page.Locator( "body>div>div>div>div[role='button'][title='Stop']:not(:disabled)" ).WaitForAsync( new()
    {
        State = WaitForSelectorState.Visible,
        Timeout = 30_000
    } );

    await Task.Delay( TimeSpan.FromSeconds( 15 ) );
}

static async Task<WadioApplicationFactory> StartServer( )
{
    var factory = new WadioApplicationFactory();
    try
    {
        factory.StartServer();
        using( var client = factory.CreateClient() )
        {
            var response = await client.GetAsync( "/" );
            response.EnsureSuccessStatusCode();
        }

        return factory;
    }
    catch
    {
        await factory.DisposeAsync();
        throw;
    }
}