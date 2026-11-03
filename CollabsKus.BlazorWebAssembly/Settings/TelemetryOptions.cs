namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class TelemetryOptions : IAppOptions
{
   public const string DefaultEnvironment = "Production";

   public static string SectionName => "ApplicationInsights";

   public string? ConnectionString { get; set; }

   public string SdkUrl { get; set; } = string.Empty;

   public string CloudRole { get; set; } = string.Empty;

   public int MaxQueueLength { get; set; }

   public string Environment { get; set; } = DefaultEnvironment;

   public bool IsEnabled => !string.IsNullOrWhiteSpace(ConnectionString);

   public IEnumerable<string> Validate()
   {
      if (!IsEnabled)
      {
         yield break;
      }

      if (!OptionsGuard.IsHttpUrl(SdkUrl))
      {
         yield return "SdkUrl must be an absolute http(s) URL.";
      }

      if (string.IsNullOrWhiteSpace(CloudRole))
      {
         yield return "CloudRole is required.";
      }

      if (MaxQueueLength < 1)
      {
         yield return "MaxQueueLength must be at least 1.";
      }
   }
}
