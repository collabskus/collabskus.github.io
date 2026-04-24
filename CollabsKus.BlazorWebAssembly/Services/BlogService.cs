using System.Net.Http.Json;
using CollabsKus.BlazorWebAssembly.Models;
using Markdig;

namespace CollabsKus.BlazorWebAssembly.Services;

public class BlogService(HttpClient httpClient)
{
    private BlogManifest? _cachedManifest;
    private DateTime? _manifestCacheTime;
    private readonly Dictionary<string, BlogPost> _postCache = new();
    private readonly Dictionary<string, BlogAuthor> _authorCache = new();

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .Build();

    public async Task<List<BlogPostSummary>> GetPostsAsync()
    {
        var manifest = await GetManifestAsync();
        return [.. manifest.Posts.OrderByDescending(p => p.Date)];
    }

    public async Task<BlogPost?> GetPostAsync(string slug)
    {
        if (_postCache.TryGetValue(slug, out var cached))
            return cached;

        try
        {
            var raw = await httpClient.GetStringAsync($"blog/posts/{slug}.md");
            var post = ParsePost(slug, raw);
            _postCache[slug] = post;
            return post;
        }
        catch
        {
            return null;
        }
    }

    public async Task<BlogAuthor?> GetAuthorAsync(string authorId)
    {
        if (_authorCache.TryGetValue(authorId, out var cached))
            return cached;

        try
        {
            var author = await httpClient.GetFromJsonAsync<BlogAuthor>($"blog/authors/{authorId}.json");
            if (author != null)
                _authorCache[authorId] = author;
            return author;
        }
        catch
        {
            return null;
        }
    }

    private async Task<BlogManifest> GetManifestAsync()
    {
        if (_cachedManifest != null && _manifestCacheTime.HasValue &&
            DateTime.UtcNow - _manifestCacheTime.Value < TimeSpan.FromMinutes(5))
            return _cachedManifest;

        try
        {
            var manifest = await httpClient.GetFromJsonAsync<BlogManifest>("blog/manifest.json");
            _cachedManifest = manifest ?? new BlogManifest();
        }
        catch
        {
            _cachedManifest = new BlogManifest();
        }
        _manifestCacheTime = DateTime.UtcNow;
        return _cachedManifest;
    }

    public static BlogPost ParsePost(string slug, string raw)
    {
        var (front, body) = SplitFrontmatter(raw);
        var contentHtml = Markdown.ToHtml(body.Trim(), Pipeline);

        return new BlogPost
        {
            Slug = slug,
            Title = front.GetValueOrDefault("title", slug),
            Date = front.GetValueOrDefault("date", string.Empty),
            Author = front.GetValueOrDefault("author", string.Empty),
            Excerpt = front.GetValueOrDefault("excerpt", string.Empty),
            ContentHtml = contentHtml
        };
    }

    public static (Dictionary<string, string> Frontmatter, string Body) SplitFrontmatter(string raw)
    {
        var front = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!raw.StartsWith("---"))
            return (front, raw);

        var end = raw.IndexOf("\n---", 3);
        if (end < 0)
            return (front, raw);

        var yaml = raw[3..end].Trim();
        var body = raw[(end + 4)..];

        foreach (var line in yaml.Split('\n'))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;
            var key = line[..colonIdx].Trim();
            var val = line[(colonIdx + 1)..].Trim();
            if (!string.IsNullOrEmpty(key))
                front[key] = val;
        }

        return (front, body);
    }
}
