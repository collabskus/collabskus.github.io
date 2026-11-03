namespace CollabsKus.BlazorWebAssembly.Services;

public interface ITelemetryService
{
   Task InitializeAsync();

   Task TrackPageViewAsync(string? name, string? uri);

   Task TrackEventAsync(string name, IReadOnlyDictionary<string, string>? properties = null);

   Task TrackExceptionAsync(Exception exception, IReadOnlyDictionary<string, string>? properties = null);

   Task TrackTraceAsync(string message, TelemetrySeverity severity = TelemetrySeverity.Information, IReadOnlyDictionary<string, string>? properties = null);

   Task FlushAsync();
}
