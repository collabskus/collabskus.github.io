using Microsoft.JSInterop;

namespace CollabsKus.BlazorWebAssembly.Services;

/// <summary>
/// Thin wrapper around the Application Insights JavaScript SDK loaded in index.html.
/// All methods are fire-and-forget: telemetry failures must never break the app.
/// </summary>
public class TelemetryService(IJSRuntime jsRuntime)
{
   private bool _booted;

   /// <summary>
   /// Pass the connection string to the JS SDK. Called once from Program.cs
   /// after the WebAssemblyHost is built and configuration is available.
   /// </summary>
   public async Task BootAsync(string? connectionString)
   {
      if (_booted) return;
      if (string.IsNullOrWhiteSpace(connectionString)) return;
      try
      {
         await jsRuntime.InvokeVoidAsync("appInsights.boot", connectionString);
         _booted = true;
      }
      catch
      {
         // Silent — telemetry is non-critical.
      }
   }

   public async Task TrackEventAsync(string name, IDictionary<string, string>? properties = null)
   {
      try
      {
         await jsRuntime.InvokeVoidAsync("appInsights.trackEvent", name, properties);
      }
      catch { }
   }

   public async Task TrackPageViewAsync(string? name, string? uri)
   {
      try
      {
         await jsRuntime.InvokeVoidAsync("appInsights.trackPageView", name, uri);
      }
      catch { }
   }

   public async Task TrackExceptionAsync(Exception ex, IDictionary<string, string>? properties = null)
   {
      try
      {
         await jsRuntime.InvokeVoidAsync(
             "appInsights.trackException",
             ex.Message,
             ex.ToString(),
             properties);
      }
      catch { }
   }

   public async Task TrackTraceAsync(string message, int severityLevel = 1, IDictionary<string, string>? properties = null)
   {
      // Severity: 0=Verbose, 1=Information, 2=Warning, 3=Error, 4=Critical
      try
      {
         await jsRuntime.InvokeVoidAsync("appInsights.trackTrace", message, severityLevel, properties);
      }
      catch { }
   }

   public async Task FlushAsync()
   {
      try
      {
         await jsRuntime.InvokeVoidAsync("appInsights.flush");
      }
      catch { }
   }
}
