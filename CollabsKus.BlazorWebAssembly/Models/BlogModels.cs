using System.Text.Json.Serialization;

namespace CollabsKus.BlazorWebAssembly.Models;

public class BlogManifest
{
    [JsonPropertyName("posts")]
    public List<BlogPostSummary> Posts { get; set; } = new();
}

public class BlogPostSummary
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;
}

public class BlogPost : BlogPostSummary
{
    public string ContentHtml { get; set; } = string.Empty;
}

public class BlogAuthor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}
