namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class BlogOptions : IAppOptions
{
   public static string SectionName => "Blog";

   public string ManifestPath { get; set; } = string.Empty;

   public string PostsPath { get; set; } = string.Empty;

   public string AuthorsPath { get; set; } = string.Empty;

   public int ManifestCacheMinutes { get; set; }

   public TimeSpan ManifestCacheDuration => TimeSpan.FromMinutes(ManifestCacheMinutes);

   public IEnumerable<string> Validate()
   {
      if (!OptionsGuard.IsRelativePath(ManifestPath))
      {
         yield return "ManifestPath must be a relative path.";
      }

      if (!OptionsGuard.IsRelativePath(PostsPath))
      {
         yield return "PostsPath must be a relative path.";
      }

      if (!OptionsGuard.IsRelativePath(AuthorsPath))
      {
         yield return "AuthorsPath must be a relative path.";
      }

      if (ManifestCacheMinutes < 0)
      {
         yield return "ManifestCacheMinutes must not be negative.";
      }
   }
}
