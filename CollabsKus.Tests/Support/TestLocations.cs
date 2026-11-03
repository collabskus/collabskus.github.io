using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.Tests.Support;

internal static class TestLocations
{
   public static readonly TimeSpan NepalOffset = TimeSpan.FromMinutes(345);

   public static readonly GeoLocation Kathmandu = new("Kathmandu, Nepal", 27.6984037, 85.2939889, NepalOffset);
}
