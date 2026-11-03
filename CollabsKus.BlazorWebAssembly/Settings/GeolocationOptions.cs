namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class GeolocationOptions : IAppOptions
{
   public static string SectionName => "Geolocation";

   public bool Enabled { get; set; }

   public bool EnableHighAccuracy { get; set; }

   public int TimeoutMilliseconds { get; set; }

   public int MaximumAgeMilliseconds { get; set; }

   public string Label { get; set; } = string.Empty;

   public IEnumerable<string> Validate()
   {
      if (!Enabled)
      {
         yield break;
      }

      if (TimeoutMilliseconds < 1)
      {
         yield return "TimeoutMilliseconds must be positive.";
      }

      if (MaximumAgeMilliseconds < 0)
      {
         yield return "MaximumAgeMilliseconds must not be negative.";
      }

      if (string.IsNullOrWhiteSpace(Label))
      {
         yield return "Label is required.";
      }
   }
}
