namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class ApiLoggerOptions : IAppOptions
{
   public const int MinimumContentLength = 16;

   public static string SectionName => "ApiLogger";

   public bool Enabled { get; set; }

   public string NotesUrl { get; set; } = string.Empty;

   public string VaultUrl { get; set; } = string.Empty;

   public string PageUrl { get; set; } = string.Empty;

   public string UserAgentLabel { get; set; } = string.Empty;

   public int MaxContentLength { get; set; }

   public IEnumerable<string> Validate()
   {
      if (!Enabled)
      {
         yield break;
      }

      if (!OptionsGuard.IsHttpUrl(NotesUrl))
      {
         yield return "NotesUrl must be an absolute http(s) URL.";
      }

      if (!OptionsGuard.IsHttpUrl(VaultUrl))
      {
         yield return "VaultUrl must be an absolute http(s) URL.";
      }

      if (!OptionsGuard.IsHttpUrl(PageUrl))
      {
         yield return "PageUrl must be an absolute http(s) URL.";
      }

      if (string.IsNullOrWhiteSpace(UserAgentLabel))
      {
         yield return "UserAgentLabel is required.";
      }

      if (MaxContentLength < MinimumContentLength)
      {
         yield return $"MaxContentLength must be at least {MinimumContentLength}.";
      }
   }
}
