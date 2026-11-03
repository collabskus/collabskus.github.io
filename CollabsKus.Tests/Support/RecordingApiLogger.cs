using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Support;

internal sealed class RecordingApiLogger : IApiLogger
{
   public List<(string Endpoint, bool FromCache)> Entries { get; } = [];

   public void LogApiRequest(string endpoint, object? data, bool fromCache) => Entries.Add((endpoint, fromCache));
}
