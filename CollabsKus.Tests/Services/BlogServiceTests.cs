using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Services;

public class BlogServiceTests
{
    // ── SplitFrontmatter ──────────────────────────────────────────────────

    [Test]
    public async Task SplitFrontmatter_NoDelimiters_ReturnsEmptyFrontAndFullBody()
    {
        var raw = "# Hello\n\nNo frontmatter here.";
        var (front, body) = BlogService.SplitFrontmatter(raw);
        await Assert.That(front.Count).IsEqualTo(0);
        await Assert.That(body).IsEqualTo(raw);
    }

    [Test]
    public async Task SplitFrontmatter_ValidFrontmatter_ExtractsKeys()
    {
        var raw = "---\ntitle: Hello World\ndate: 2026-04-24\nauthor: collabskus\n---\n\n# Body";
        var (front, body) = BlogService.SplitFrontmatter(raw);
        await Assert.That(front["title"]).IsEqualTo("Hello World");
        await Assert.That(front["date"]).IsEqualTo("2026-04-24");
        await Assert.That(front["author"]).IsEqualTo("collabskus");
    }

    [Test]
    public async Task SplitFrontmatter_ValidFrontmatter_BodyExcludesDelimiters()
    {
        var raw = "---\ntitle: Test\n---\n\nBody content.";
        var (_, body) = BlogService.SplitFrontmatter(raw);
        await Assert.That(body.Contains("---")).IsFalse();
        await Assert.That(body.Trim()).IsEqualTo("Body content.");
    }

    [Test]
    public async Task SplitFrontmatter_KeysAreCaseInsensitive()
    {
        var raw = "---\nTitle: My Post\n---\n\nContent.";
        var (front, _) = BlogService.SplitFrontmatter(raw);
        await Assert.That(front.ContainsKey("title")).IsTrue();
        await Assert.That(front["title"]).IsEqualTo("My Post");
    }

    [Test]
    public async Task SplitFrontmatter_ValueWithColon_PreservesRemainder()
    {
        var raw = "---\nexcerpt: See https://example.com for more\n---\n\nContent.";
        var (front, _) = BlogService.SplitFrontmatter(raw);
        await Assert.That(front["excerpt"]).IsEqualTo("See https://example.com for more");
    }

    [Test]
    public async Task SplitFrontmatter_MissingClosingDelimiter_ReturnsEmptyFrontAndFullBody()
    {
        var raw = "---\ntitle: Test\n\nContent with no closing delimiter.";
        var (front, body) = BlogService.SplitFrontmatter(raw);
        await Assert.That(front.Count).IsEqualTo(0);
        await Assert.That(body).IsEqualTo(raw);
    }

    // ── ParsePost ─────────────────────────────────────────────────────────

    [Test]
    public async Task ParsePost_ExtractsTitle()
    {
        var md = "---\ntitle: Hello World\ndate: 2026-04-24\nauthor: collabskus\nexcerpt: Test.\n---\n\n# Hello\n";
        var post = BlogService.ParsePost("hello-world", md);
        await Assert.That(post.Title).IsEqualTo("Hello World");
    }

    [Test]
    public async Task ParsePost_ExtractsSlug()
    {
        var md = "---\ntitle: Hello World\n---\n\nContent.";
        var post = BlogService.ParsePost("hello-world", md);
        await Assert.That(post.Slug).IsEqualTo("hello-world");
    }

    [Test]
    public async Task ParsePost_ExtractsDate()
    {
        var md = "---\ntitle: T\ndate: 2026-04-24\n---\n\nContent.";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.Date).IsEqualTo("2026-04-24");
    }

    [Test]
    public async Task ParsePost_ExtractsExcerpt()
    {
        var md = "---\ntitle: T\nexcerpt: Short summary here.\n---\n\nContent.";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.Excerpt).IsEqualTo("Short summary here.");
    }

    [Test]
    public async Task ParsePost_RendersMarkdownToHtml()
    {
        var md = "---\ntitle: T\n---\n\n# Heading\n\nParagraph text.";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.ContentHtml.Contains("<h1>")).IsTrue();
        await Assert.That(post.ContentHtml.Contains("<p>")).IsTrue();
    }

    [Test]
    public async Task ParsePost_RendersStrongEmphasis()
    {
        var md = "---\ntitle: T\n---\n\n**bold** and *italic*";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.ContentHtml.Contains("<strong>")).IsTrue();
        await Assert.That(post.ContentHtml.Contains("<em>")).IsTrue();
    }

    [Test]
    public async Task ParsePost_NoFrontmatter_SlugUsedAsTitle()
    {
        var md = "# Just Content\n\nNo frontmatter.";
        var post = BlogService.ParsePost("my-slug", md);
        await Assert.That(post.Title).IsEqualTo("my-slug");
        await Assert.That(post.Slug).IsEqualTo("my-slug");
    }

    [Test]
    public async Task ParsePost_RendersLists()
    {
        var md = "---\ntitle: T\n---\n\n- Item one\n- Item two\n";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.ContentHtml.Contains("<ul>") || post.ContentHtml.Contains("<li>")).IsTrue();
    }

    [Test]
    public async Task ParsePost_RendersLinks()
    {
        var md = "---\ntitle: T\n---\n\n[Click here](https://example.com)";
        var post = BlogService.ParsePost("test", md);
        await Assert.That(post.ContentHtml.Contains("<a ")).IsTrue();
        await Assert.That(post.ContentHtml.Contains("https://example.com")).IsTrue();
    }
}
