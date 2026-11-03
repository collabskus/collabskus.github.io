using System.Net.Http.Json;
using System.Text.Json;
using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Settings;
using Markdig;
using Microsoft.Extensions.Options;

namespace CollabsKus.BlazorWebAssembly.Services;

public class BlogService(HttpClient httpClient, IOptions<BlogOptions> options, TimeProvider timeProvider)
{
   private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
       .UseAutoLinks()
       .UseEmphasisExtras()
       .Build();

   private readonly BlogOptions _options = options.Value;
   private readonly Dictionary<string, BlogPost> _postCache = new(StringComparer.Ordinal);
   private readonly Dictionary<string, BlogAuthor> _authorCache = new(StringComparer.Ordinal);
   private BlogManifest? _cachedManifest;
   private DateTimeOffset _manifestCachedAt;

   public async Task<List<BlogPostSummary>> GetPostsAsync()
   {
      var manifest = await GetManifestAsync();
      return [.. manifest.Posts.OrderByDescending(p => p.Date)];
   }

   public async Task<BlogPost?> GetPostAsync(string slug)
   {
      if (!IsValidSlug(slug))
         return null;

      if (_postCache.TryGetValue(slug, out var cached))
         return cached;

      try
      {
         string raw = await httpClient.GetStringAsync(CombinePath(_options.PostsPath, $"{slug}.md"));
         var post = ParsePost(slug, raw);
         _postCache[slug] = post;
         return post;
      }
      catch (Exception ex) when (IsRecoverable(ex))
      {
         return null;
      }
   }

   public async Task<BlogAuthor?> GetAuthorAsync(string authorId)
   {
      if (!IsValidSlug(authorId))
         return null;

      if (_authorCache.TryGetValue(authorId, out var cached))
         return cached;

      try
      {
         var author = await httpClient.GetFromJsonAsync<BlogAuthor>(CombinePath(_options.AuthorsPath, $"{authorId}.json"));
         if (author != null)
            _authorCache[authorId] = author;
         return author;
      }
      catch (Exception ex) when (IsRecoverable(ex))
      {
         return null;
      }
   }

   public static bool IsValidSlug(string? value) =>
      !string.IsNullOrWhiteSpace(value)
      && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

   public static BlogPost ParsePost(string slug, string raw)
   {
      var (front, body) = SplitFrontmatter(raw);
      string contentHtml = Markdown.ToHtml(body.Trim(), Pipeline);

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

      int end = raw.IndexOf("\n---", 3);
      if (end < 0)
         return (front, raw);

      string yaml = raw[3..end].Trim();
      string body = raw[(end + 4)..];

      foreach (string line in yaml.Split('\n'))
      {
         int colonIdx = line.IndexOf(':');
         if (colonIdx < 0) continue;
         string key = line[..colonIdx].Trim();
         string val = line[(colonIdx + 1)..].Trim();
         if (!string.IsNullOrEmpty(key))
            front[key] = val;
      }

      return (front, body);
   }

   private async Task<BlogManifest> GetManifestAsync()
   {
      var now = timeProvider.GetUtcNow();
      if (_cachedManifest != null && now - _manifestCachedAt < _options.ManifestCacheDuration)
         return _cachedManifest;

      try
      {
         var manifest = await httpClient.GetFromJsonAsync<BlogManifest>(_options.ManifestPath);
         if (manifest != null)
         {
            _cachedManifest = manifest;
            _manifestCachedAt = now;
            return manifest;
         }
      }
      catch (Exception ex) when (IsRecoverable(ex))
      {
      }

      return _cachedManifest ?? new BlogManifest();
   }

   private static string CombinePath(string basePath, string fileName) => $"{basePath.TrimEnd('/')}/{fileName}";

   private static bool IsRecoverable(Exception ex) =>
      ex is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException;
}
