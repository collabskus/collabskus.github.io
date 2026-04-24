namespace CollabsKus.PlaywrightTests;

public class BlogTests
{
    private static readonly bool ShouldManageServer =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BASE_URL"));

    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? TestServerManager.DefaultBaseUrl;

    private const int TimeoutMs = 30_000;

    private static IPlaywright _playwright = null!;
    private static IBrowser _browser = null!;

    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // ── Class lifecycle ────────────────────────────────────────────────────

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

    // ── Test lifecycle ─────────────────────────────────────────────────────

    [Before(Test)]
    public async Task SetupPage()
    {
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(TimeoutMs);
    }

    [After(Test)]
    public async Task TeardownPage()
    {
        await _context.CloseAsync();
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public async Task BlogList_HasCorrectTitle()
    {
        // Must wait for Blazor to render the component before <PageTitle> has updated
        // document.title from the index.html default "Kathmandu Calendar & Time".
        await NavigateToBlogListAsync();
        await Assert.That(await _page.TitleAsync()).Contains("Blog");
    }

    [Test]
    public async Task BlogList_ShowsPostList()
    {
        await NavigateToBlogListAsync();
        var count = await _page.Locator(".post-card").CountAsync();
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task BlogList_ShowsHelloWorldPost()
    {
        await NavigateToBlogListAsync();
        var titles = await _page.Locator(".post-title").AllTextContentsAsync();
        await Assert.That(titles.Any(t => t.Contains("Hello World"))).IsTrue();
    }

    [Test]
    public async Task BlogList_PostCardHasExcerpt()
    {
        await NavigateToBlogListAsync();
        var excerpt = await _page.Locator(".post-excerpt").First.TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(excerpt)).IsFalse();
    }

    [Test]
    public async Task BlogList_PostCardHasDate()
    {
        await NavigateToBlogListAsync();
        var date = await _page.Locator(".post-date").First.TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(date)).IsFalse();
    }

    [Test]
    public async Task BlogDetail_NavigatesFromList()
    {
        await NavigateToBlogListAsync();
        await _page.Locator(".post-card").First.ClickAsync();
        await _page.Locator(".blog-post").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assert.That(await _page.Locator(".blog-post").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogDetail_ShowsPostTitle()
    {
        await NavigateToBlogDetailAsync("hello-world");
        var title = await _page.Locator(".post-title").TextContentAsync();
        await Assert.That(title!.Contains("Hello World")).IsTrue();
    }

    [Test]
    public async Task BlogDetail_ShowsPostContent()
    {
        await NavigateToBlogDetailAsync("hello-world");
        var content = await _page.Locator(".post-content").TextContentAsync();
        await Assert.That(string.IsNullOrWhiteSpace(content)).IsFalse();
    }

    [Test]
    public async Task BlogDetail_ShowsAuthor()
    {
        await NavigateToBlogDetailAsync("hello-world");
        var author = await _page.Locator(".post-author").TextContentAsync();
        await Assert.That(author!.Contains("CollabsKus")).IsTrue();
    }

    [Test]
    public async Task BlogDetail_HasBackLink()
    {
        await NavigateToBlogDetailAsync("hello-world");
        // .back-link is the "← Blog" link; .blog-home-link is the "Home" link
        var backLink = _page.Locator(".back-link");
        await backLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await backLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogDetail_HasHomeLink()
    {
        await NavigateToBlogDetailAsync("hello-world");
        var homeLink = _page.Locator(".blog-home-link");
        await homeLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await homeLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogDetail_BackLinkNavigatesToList()
    {
        await NavigateToBlogDetailAsync("hello-world");
        // Target the Blog link specifically — the post-nav also contains a Home link
        await _page.Locator(".back-link[href='/blog']").ClickAsync();
        await _page.Locator(".blog-list").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assert.That(await _page.Locator(".blog-list").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogList_HasHomeLink()
    {
        await NavigateToBlogListAsync();
        var homeLink = _page.Locator(".blog-home-link");
        await homeLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Assert.That(await homeLink.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogList_HomeLinkNavigatesHome()
    {
        await NavigateToBlogListAsync();
        await _page.Locator(".blog-home-link").ClickAsync();
        // Home page renders .footer immediately after Blazor bootstraps
        await _page.Locator(".footer").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assert.That(await _page.TitleAsync()).IsEqualTo("Kathmandu Calendar & Time");
    }

    [Test]
    public async Task BlogDetail_UnknownSlug_ShowsNotFound()
    {
        await _page.GotoAsync($"{BaseUrl}/blog/this-post-does-not-exist", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = TimeoutMs
        });
        await _page.Locator(".not-found").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assert.That(await _page.Locator(".not-found").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task BlogDetail_HasCorrectPageTitle()
    {
        await NavigateToBlogDetailAsync("hello-world");
        var title = await _page.TitleAsync();
        await Assert.That(title.Contains("Hello World")).IsTrue();
    }

    // ── Navigation helpers ─────────────────────────────────────────────────

    private async Task NavigateToBlogListAsync()
    {
        await _page.GotoAsync($"{BaseUrl}/blog", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = TimeoutMs
        });
        await _page.Locator(".blog-list").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        // Wait for either post list or the empty state to appear
        await _page.WaitForFunctionAsync(
            "() => document.querySelector('.post-list') || document.querySelector('.no-posts')",
            null,
            new PageWaitForFunctionOptions { Timeout = TimeoutMs }
        );
    }

    private async Task NavigateToBlogDetailAsync(string slug)
    {
        await _page.GotoAsync($"{BaseUrl}/blog/{slug}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = TimeoutMs
        });
        await _page.Locator(".blog-post, .not-found").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }
}
