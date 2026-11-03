namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class CalendarApiOptions : IAppOptions
{
   public static string SectionName => "CalendarApi";

   public string TodayUrl { get; set; } = string.Empty;

   public string TimeUrl { get; set; } = string.Empty;

   public int RequestTimeoutSeconds { get; set; }

   public int ClockOffsetCacheMinutes { get; set; }

   public TimeSpan RequestTimeout => TimeSpan.FromSeconds(RequestTimeoutSeconds);

   public TimeSpan ClockOffsetCacheDuration => TimeSpan.FromMinutes(ClockOffsetCacheMinutes);

   public IEnumerable<string> Validate()
   {
      if (!OptionsGuard.IsHttpUrl(TodayUrl))
      {
         yield return "TodayUrl must be an absolute http(s) URL.";
      }

      if (!OptionsGuard.IsHttpUrl(TimeUrl))
      {
         yield return "TimeUrl must be an absolute http(s) URL.";
      }

      if (RequestTimeoutSeconds is < 1 or > 120)
      {
         yield return "RequestTimeoutSeconds must be between 1 and 120.";
      }

      if (ClockOffsetCacheMinutes is < 1 or > 10080)
      {
         yield return "ClockOffsetCacheMinutes must be between 1 and 10080.";
      }
   }
}
