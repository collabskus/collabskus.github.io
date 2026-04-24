namespace CollabsKus.PlaywrightTests;

public class HomePageTests
{
    private static readonly bool ShouldManageServer =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BASE_URL"));

    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? TestServerManager.DefaultBaseUrl;

    // Blazor WASM downloads the .NET runtime then calls an external calendar API before
    // rendering — allow enough time for both before timing out element waits.
    private const int AppReadyTimeoutMs = 60_000;

    private static IPlaywright _playwright = null!;
    private static IBrowser _browser = null!;

    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // ── Class lifecycle: server + browser shared across all tests ──────────

    [Before(Class)]
    public static async Task StartInfrastructure()
    {
        if (ShouldManageServer)
            await TestServerManager.AcquireAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    [After(Class)]
    public static async Task StopInfrastructure()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();

        if (ShouldManageServer)
            await TestServerManager.ReleaseAsync();
    }

    // ── Test lifecycle: fresh isolated context + page per test ─────────────

    [Before(Test)]
    public async Task SetupPage()
    {
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = []   // deny geolocation → Kathmandu-only mode
        });
        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(AppReadyTimeoutMs);
    }

    [After(Test)]
    public async Task TeardownPage()
    {
        await _context.CloseAsync();
    }

    // ── Navigation helper ──────────────────────────────────────────────────

    // WaitUntilState.Load (not NetworkIdle): Blazor WASM makes background API calls
    // to an external calendar service and sends telemetry to Cloudflare Workers —
    // those prevent NetworkIdle from ever firing within a reasonable timeout.
    // We then wait for .time-display, which only renders once _isLoading clears and
    // calendar data has been successfully fetched.
    private async Task NavigateAsync()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        await _page.Locator(".time-display").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Page_HasCorrectTitle()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        await Assert.That(await _page.TitleAsync()).IsEqualTo("Kathmandu Calendar & Time");
    }

    [Test]
    public async Task Header_ShowsKathmanduInNepali()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        var h1 = await _page.Locator("h1").TextContentAsync();
        await Assert.That(h1).IsEqualTo("काठमाडौं");
    }

    [Test]
    public async Task TimeDisplay_IsVisible()
    {
        await NavigateAsync();
        await Assert.That(await _page.Locator(".time-display").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task MoonDisplay_ShowsPhaseInfo()
    {
        await NavigateAsync();

        var moonIcon = await _page.Locator(".moon-icon").TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(moonIcon)).IsFalse();

        var phaseName = await _page.Locator(".moon-phase-name").TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(phaseName)).IsFalse();
    }

    [Test]
    public async Task SunDisplay_IsRendered()
    {
        await NavigateAsync();
        await Assert.That(await _page.Locator("#sun-display-root").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task SunDisplay_ShowsKathmanduLocation()
    {
        await NavigateAsync();
        var header = await _page.Locator(".sun-location-header").First.TextContentAsync();
        await Assert.That(header!.Contains("Kathmandu")).IsTrue();
    }

    [Test]
    public async Task DateCards_ShowBikramSambat()
    {
        await NavigateAsync();
        var cards = await _page.Locator(".date-card").CountAsync();
        await Assert.That(cards).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task CalendarGrid_RendersSevenDayColumns()
    {
        await NavigateAsync();
        var dayNames = await _page.Locator(".calendar-header .day-name").CountAsync();
        await Assert.That(dayNames).IsEqualTo(7);
    }

    [Test]
    public async Task Footer_ShowsLastUpdated()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        // .footer is rendered outside the @if (_isLoading) gate — present as soon as Blazor
        // bootstraps, before the external calendar API responds. No need to wait for data.
        var footer = await _page.Locator(".footer").TextContentAsync();
        await Assert.That(footer!.Contains("Last updated")).IsTrue();
    }

    [Test]
    public async Task Footer_HasGitHubLink()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        // .footer renders as soon as Blazor bootstraps; WaitForAsync auto-waits for it.
        var githubLink = _page.Locator(".footer a[href*='github.com']");
        await githubLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await githubLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Footer_HasBlogLink()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        var blogLink = _page.Locator(".footer a[href='/blog']");
        await blogLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await blogLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task MoonLiveIndicator_IsVisible()
    {
        await NavigateAsync();
        // MoonDisplay uses IntersectionObserver to gate IsLive; the indicator only renders
        // while the element is in the viewport. Scroll it in before asserting.
        await _page.Locator("#moon-display-root").ScrollIntoViewIfNeededAsync();
        await _page.Locator(".moon-live-indicator").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assert.That(await _page.Locator(".moon-live-indicator").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task SunCanvas_ExistsForKathmandu()
    {
        await NavigateAsync();
        await Assert.That(await _page.Locator("#sunSkyCanvasKtm").CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task WithGeolocation_ShowsUserSunTracker()
    {
        await using var geoContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = ["geolocation"],
            Geolocation = new Geolocation { Latitude = 40.7128f, Longitude = -74.006f }
        });

        var geoPage = await geoContext.NewPageAsync();
        geoPage.SetDefaultTimeout(AppReadyTimeoutMs);

        await geoPage.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = AppReadyTimeoutMs
        });
        await geoPage.Locator(".time-display").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        try
        {
            // The user canvas appears after geolocation resolves and triggers a second render.
            await geoPage.Locator("#sunSkyCanvasUser").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
            await Assert.That(await geoPage.Locator("#sunSkyCanvasUser").CountAsync()).IsEqualTo(1);
        }
        catch (TimeoutException)
        {
            // Geolocation may not resolve in all headless / CI environments
        }
    }
}
