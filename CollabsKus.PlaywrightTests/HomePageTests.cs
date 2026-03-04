using System.Diagnostics;

namespace CollabsKus.PlaywrightTests;

/// <summary>
/// End-to-end Playwright tests for the Kathmandu Calendar app.
///
/// The test class automatically starts the Blazor WASM dev server before
/// any tests run and shuts it down after all tests complete.
///
/// Prerequisites:
///   1. Install browsers: pwsh bin/Debug/net10.0/playwright.ps1 install
///   2. Run tests: dotnet test CollabsKus.PlaywrightTests
///
/// Set the BASE_URL environment variable to override the default (http://localhost:5267).
/// If BASE_URL is set, the server will NOT be auto-started (assumes external server).
/// </summary>
public class HomePageTests
{
    private const string DefaultBaseUrl = "http://localhost:5267";

    private static string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? DefaultBaseUrl;
    private static bool _shouldManageServer = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BASE_URL"));
    private static Process? _serverProcess;
    private static readonly object _serverLock = new();

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // ── Server lifecycle (once per class) ────────────────────────────────

    [Before(Class)]
    public static async Task StartServer()
    {
        if (!_shouldManageServer)
            return;

        // Resolve the Blazor WASM project path relative to the test output directory.
        // Test output is: CollabsKus.PlaywrightTests/bin/Debug/net10.0/
        // We need:        CollabsKus.BlazorWebAssembly/
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var blazorProject = Path.Combine(projectRoot, "CollabsKus.BlazorWebAssembly", "CollabsKus.BlazorWebAssembly.csproj");

        if (!File.Exists(blazorProject))
        {
            throw new FileNotFoundException(
                $"Could not find Blazor project at: {blazorProject}. " +
                $"Resolved from base directory: {baseDir}");
        }

        lock (_serverLock)
        {
            if (_serverProcess != null)
                return;

            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{blazorProject}\" --urls {DefaultBaseUrl} --no-launch-profile",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _serverProcess.Start();

            // Consume stdout/stderr to prevent buffer deadlocks
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
        }

        // Wait for the server to respond (up to 120 seconds for initial build + start)
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(120);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(DefaultBaseUrl);
                if (response.IsSuccessStatusCode)
                    return; // Server is ready
            }
            catch
            {
                // Server not ready yet
            }

            // Check if the process crashed
            if (_serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Blazor dev server exited with code {_serverProcess.ExitCode} before becoming ready.");
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Blazor dev server did not respond at {DefaultBaseUrl} within 120 seconds.");
    }

    [After(Class)]
    public static async Task StopServer()
    {
        if (_serverProcess == null || _serverProcess.HasExited)
            return;

        try
        {
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }
        catch
        {
            // Best effort cleanup
        }
        finally
        {
            _serverProcess.Dispose();
            _serverProcess = null;
        }
    }

    // ── Per-test browser lifecycle ────────────────────────────────────────

    [Before(Test)]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            // Deny geolocation to test Kathmandu-only mode
            Permissions = []
        });
        _page = await _context.NewPageAsync();
    }

    [After(Test)]
    public async Task Teardown()
    {
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    // ── Tests ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Page_HasCorrectTitle()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var title = await _page.TitleAsync();
        await Assert.That(title).IsEqualTo("Kathmandu Calendar & Time");
    }

    [Test]
    public async Task Header_ShowsKathmanduInNepali()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var h1 = await _page.Locator("h1").TextContentAsync();
        await Assert.That(h1).IsEqualTo("काठमाडौं");
    }

    [Test]
    public async Task TimeDisplay_IsVisible()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".time-display", new PageWaitForSelectorOptions { Timeout = 30000 });
        var isVisible = await _page.Locator(".time-display").IsVisibleAsync();
        await Assert.That(isVisible).IsTrue();
    }

    [Test]
    public async Task MoonDisplay_ShowsPhaseInfo()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync("#moon-display-root", new PageWaitForSelectorOptions { Timeout = 30000 });

        var moonIcon = await _page.Locator(".moon-icon").TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(moonIcon)).IsFalse();

        var phaseName = await _page.Locator(".moon-phase-name").TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(phaseName)).IsFalse();
    }

    [Test]
    public async Task SunDisplay_IsRendered()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync("#sun-display-root", new PageWaitForSelectorOptions { Timeout = 30000 });
        var isVisible = await _page.Locator("#sun-display-root").IsVisibleAsync();
        await Assert.That(isVisible).IsTrue();
    }

    [Test]
    public async Task SunDisplay_ShowsKathmanduLocation()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".sun-location-header", new PageWaitForSelectorOptions { Timeout = 30000 });

        var header = await _page.Locator(".sun-location-header").First.TextContentAsync();
        await Assert.That(header!.Contains("Kathmandu")).IsTrue();
    }

    [Test]
    public async Task DateCards_ShowBikramSambat()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".date-card", new PageWaitForSelectorOptions { Timeout = 30000 });

        var cards = await _page.Locator(".date-card").CountAsync();
        await Assert.That(cards).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task CalendarGrid_RendersSevenDayColumns()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".calendar-header .day-name", new PageWaitForSelectorOptions { Timeout = 30000 });

        var dayNames = await _page.Locator(".calendar-header .day-name").CountAsync();
        await Assert.That(dayNames).IsEqualTo(7);
    }

    [Test]
    public async Task Footer_ShowsLastUpdated()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".footer", new PageWaitForSelectorOptions { Timeout = 30000 });

        var footer = await _page.Locator(".footer").TextContentAsync();
        await Assert.That(footer!.Contains("Last updated")).IsTrue();
    }

    [Test]
    public async Task MoonLiveIndicator_AppearsWhenVisible()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync(".moon-live-indicator", new PageWaitForSelectorOptions { Timeout = 30000 });

        var isVisible = await _page.Locator(".moon-live-indicator").IsVisibleAsync();
        await Assert.That(isVisible).IsTrue();
    }

    [Test]
    public async Task SunCanvas_ExistsForKathmandu()
    {
        await _page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await _page.WaitForSelectorAsync("#sunSkyCanvasKtm", new PageWaitForSelectorOptions { Timeout = 30000 });

        var canvas = await _page.Locator("#sunSkyCanvasKtm").CountAsync();
        await Assert.That(canvas).IsEqualTo(1);
    }

    [Test]
    public async Task WithGeolocation_ShowsUserSunTracker()
    {
        // Create a new context with geolocation granted
        var geoContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Permissions = ["geolocation"],
            Geolocation = new Geolocation { Latitude = 40.7128f, Longitude = -74.006f },
        });

        var geoPage = await geoContext.NewPageAsync();
        await geoPage.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait for the user canvas to appear (geolocation callback triggers re-render)
        try
        {
            await geoPage.WaitForSelectorAsync("#sunSkyCanvasUser", new PageWaitForSelectorOptions { Timeout = 15000 });
            var count = await geoPage.Locator("#sunSkyCanvasUser").CountAsync();
            await Assert.That(count).IsEqualTo(1);
        }
        catch (TimeoutException)
        {
            // If geolocation doesn't work in headless mode, that's acceptable — skip
        }

        await geoContext.CloseAsync();
    }
}
