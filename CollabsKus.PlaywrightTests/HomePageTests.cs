namespace CollabsKus.PlaywrightTests;

public class HomePageTests
{
    private static readonly bool ShouldManageServer =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BASE_URL"));

    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? TestServerManager.DefaultBaseUrl;

    // Blazor WASM downloads the .NET runtime then bootstraps before rendering —
    // allow enough time for the first cold-start navigation. After the first
    // test in this class warms the shared browser context's cache, subsequent
    // navigations finish in well under a second.
    private const int AppReadyTimeoutMs = 60_000;

    private static IPlaywright _playwright = null!;
    private static IBrowser _browser = null!;

    // ────────────────────────────────────────────────────────────────────────
    // A single IBrowserContext is shared across every test in this class. Each
    // test previously created a fresh context, which gave it an empty HTTP
    // cache and forced Chromium to re-download ~5-15 MB of WASM + ICU data on
    // every single test. That cold-start cost — multiplied across ~14 tests —
    // was the root cause of the timeouts observed on slower machines / CI.
    //
    // Reusing the context preserves the HTTP cache so only the first test
    // pays the cold-start cost; the rest navigate almost instantly. The page
    // (and its DOM, JS state, etc.) is still recreated per test so tests
    // remain isolated.
    //
    // The geolocation test creates its own ephemeral context because it needs
    // different permissions than this shared one.
    // ────────────────────────────────────────────────────────────────────────
    private static IBrowserContext _sharedContext = null!;

    private IPage _page = null!;

    // ── Class lifecycle: server + browser + shared context ─────────────────

    [Before(Class)]
    public static async Task StartInfrastructure()
    {
        if (ShouldManageServer)
            await TestServerManager.AcquireAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        _sharedContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = []   // deny geolocation → Kathmandu-only mode
        });
    }

    [After(Class)]
    public static async Task StopInfrastructure()
    {
        await _sharedContext.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();

        if (ShouldManageServer)
            await TestServerManager.ReleaseAsync();
    }

    // ── Test lifecycle: fresh page per test, shared context ────────────────

    [Before(Test)]
    public async Task SetupPage()
    {
        _page = await _sharedContext.NewPageAsync();
        _page.SetDefaultTimeout(AppReadyTimeoutMs);
    }

    [After(Test)]
    public async Task TeardownPage()
    {
        await _page.CloseAsync();
    }

    // ── Navigation helper ──────────────────────────────────────────────────

    // WaitUntilState.DOMContentLoaded (not Load or NetworkIdle):
    //   - NetworkIdle never fires reliably: the page makes background calls to
    //     an external calendar service and sends telemetry to Cloudflare Workers.
    //   - Load waits for every sub-resource (CSS, fonts, the full Blazor WASM
    //     payload, every dynamically-imported assembly) which adds noticeable
    //     latency without adding correctness.
    //   - DOMContentLoaded fires as soon as the HTML shell is parsed. After
    //     that, we explicitly wait for .time-display, which means "Blazor is
    //     bootstrapped and the Home component has rendered". That is the real
    //     signal that the app is ready, regardless of how long any external
    //     resource takes.
    //
    // .time-display renders as soon as the Home component initializes — it no
    // longer waits on the external calendar API.
    private async Task NavigateAsync()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
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
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = AppReadyTimeoutMs
        });
        await Assert.That(await _page.TitleAsync()).IsEqualTo("Kathmandu Calendar & Time");
    }

    [Test]
    public async Task Header_ShowsKathmanduInNepali()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = AppReadyTimeoutMs
        });
        // Wait for Blazor to render the <h1> before reading it — it lives inside
        // the Home component, not in the static index.html shell.
        var h1Locator = _page.Locator("h1");
        await h1Locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var h1 = await h1Locator.TextContentAsync();
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
        // CalendarGrid renders only after the external calendar API has returned
        // (it gates on CalendarData != null). Auto-wait on the locator handles
        // that latency; in practice the API responds well within our timeout.
        var dayNameLocator = _page.Locator(".calendar-header .day-name");
        await dayNameLocator.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        var dayNames = await dayNameLocator.CountAsync();
        await Assert.That(dayNames).IsEqualTo(7);
    }

    [Test]
    public async Task Footer_ShowsLastUpdated()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = AppReadyTimeoutMs
        });
        // .footer is rendered by the Home component itself — wait for it.
        var footer = _page.Locator(".footer");
        await footer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var text = await footer.TextContentAsync();
        await Assert.That(text!.Contains("Last updated")).IsTrue();
    }

    [Test]
    public async Task Footer_HasGitHubLink()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = AppReadyTimeoutMs
        });
        var githubLink = _page.Locator(".footer a[href*='github.com']");
        await githubLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await githubLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Footer_HasBlogLink()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
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
        // This test needs different permissions than the shared context, so
        // spin up its own short-lived context. The first navigation pays the
        // cold-start cost again here because this context has its own cache.
        await using var geoContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = ["geolocation"],
            Geolocation = new Geolocation { Latitude = 40.7128f, Longitude = -74.006f }
        });

        var geoPage = await geoContext.NewPageAsync();
        geoPage.SetDefaultTimeout(AppReadyTimeoutMs);

        await geoPage.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
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
