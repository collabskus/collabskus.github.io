using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public interface IGeolocationService
{
   Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
