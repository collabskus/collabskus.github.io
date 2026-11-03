using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class GeolocationService(IJSRuntime jsRuntime, IOptions<GeolocationOptions> options) : IGeolocationService
{
   private readonly GeolocationOptions _options = options.Value;

   public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
   {
      if (!_options.Enabled)
      {
         return null;
      }

      BrowserPosition? position;
      try
      {
         position = await jsRuntime.InvokeAsync<BrowserPosition?>(
            "appInterop.getLocation",
            cancellationToken,
            new
            {
               enableHighAccuracy = _options.EnableHighAccuracy,
               timeout = _options.TimeoutMilliseconds,
               maximumAge = _options.MaximumAgeMilliseconds
            });
      }
      catch (JSException)
      {
         return null;
      }
      catch (OperationCanceledException)
      {
         return null;
      }

      if (position is null || position.Latitude is < -90 or > 90 || position.Longitude is < -180 or > 180)
      {
         return null;
      }

      string name = string.IsNullOrWhiteSpace(position.TimeZone)
         ? _options.Label
         : $"{_options.Label} ({position.TimeZone})";

      return new GeoLocation(name, position.Latitude, position.Longitude, TimeSpan.FromMinutes(position.OffsetMinutes));
   }

   private sealed class BrowserPosition
   {
      public double Latitude { get; set; }

      public double Longitude { get; set; }

      public int OffsetMinutes { get; set; }

      public string? TimeZone { get; set; }
   }
}
