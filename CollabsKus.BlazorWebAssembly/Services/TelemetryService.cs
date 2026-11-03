using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class TelemetryService : ITelemetryService, IAsyncDisposable
{
   private const string ModulePath = "./js/telemetry.js";

   private readonly TelemetryOptions _options;
   private readonly Lazy<Task<IJSObjectReference>> _module;

   public TelemetryService(IJSRuntime jsRuntime, IOptions<TelemetryOptions> options)
   {
      _options = ResolveOptions(options);
      _module = new Lazy<Task<IJSObjectReference>>(
         () => jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
   }

   public Task InitializeAsync() =>
      InvokeAsync("init", new
      {
         connectionString = _options.ConnectionString,
         environment = _options.Environment,
         sdkUrl = _options.SdkUrl,
         cloudRole = _options.CloudRole,
         maxQueueLength = _options.MaxQueueLength
      });

   public Task TrackPageViewAsync(string? name, string? uri) =>
      InvokeAsync("trackPageView", name, uri);

   public Task TrackEventAsync(string name, IReadOnlyDictionary<string, string>? properties = null) =>
      InvokeAsync("trackEvent", name, properties);

   public Task TrackExceptionAsync(Exception exception, IReadOnlyDictionary<string, string>? properties = null) =>
      InvokeAsync("trackException", exception.Message, exception.ToString(), exception.GetType().FullName, properties);

   public Task TrackTraceAsync(string message, TelemetrySeverity severity = TelemetrySeverity.Information, IReadOnlyDictionary<string, string>? properties = null) =>
      InvokeAsync("trackTrace", message, (int)severity, properties);

   public Task FlushAsync() => InvokeAsync("flush");

   public async ValueTask DisposeAsync()
   {
      if (!_module.IsValueCreated)
      {
         return;
      }

      try
      {
         var module = await _module.Value;
         await module.DisposeAsync();
      }
      catch
      {
      }
   }

   private static TelemetryOptions ResolveOptions(IOptions<TelemetryOptions> options)
   {
      try
      {
         return options.Value;
      }
      catch (OptionsValidationException)
      {
         return new TelemetryOptions();
      }
   }

   private async Task InvokeAsync(string identifier, params object?[] args)
   {
      if (!_options.IsEnabled)
      {
         return;
      }

      try
      {
         var module = await _module.Value;
         await module.InvokeVoidAsync(identifier, args);
      }
      catch
      {
      }
   }
}
