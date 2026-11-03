using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class LocationOptions : IAppOptions
{
   public static string SectionName => "Location";

   public string Name { get; set; } = string.Empty;

   public double Latitude { get; set; }

   public double Longitude { get; set; }

   public int UtcOffsetMinutes { get; set; }

   public TimeSpan UtcOffset => TimeSpan.FromMinutes(UtcOffsetMinutes);

   public GeoLocation ToGeoLocation() => new(Name, Latitude, Longitude, UtcOffset);

   public IEnumerable<string> Validate()
   {
      if (string.IsNullOrWhiteSpace(Name))
      {
         yield return "Name is required.";
      }

      if (Latitude is < -90 or > 90)
      {
         yield return "Latitude must be between -90 and 90.";
      }

      if (Longitude is < -180 or > 180)
      {
         yield return "Longitude must be between -180 and 180.";
      }

      if (UtcOffsetMinutes is < -720 or > 840)
      {
         yield return "UtcOffsetMinutes must be between -720 and 840.";
      }
   }
}
