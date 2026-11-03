using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using CollabsKus.BlazorWebAssembly;
using CollabsKus.BlazorWebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register our services
builder.Services.AddScoped<KathmanduCalendarService>();
builder.Services.AddScoped<MoonPhaseService>();
builder.Services.AddSingleton<SolarPositionService>();
builder.Services.AddScoped<ApiLoggerService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<TelemetryService>();

var host = builder.Build();

// ── Application Insights bootstrap ───────────────────────────────────────────
// Push the connection string from appsettings.json into the JS SDK loaded in
// index.html. The snippet queues calls until boot completes, so anything that
// fires before this point (including the initial page view auto-tracked by
// the snippet) gets the right endpoint once boot runs.
var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
var telemetry = host.Services.GetRequiredService<TelemetryService>();
await telemetry.BootAsync(connectionString);

// ── SPA navigation tracking ──────────────────────────────────────────────────
// The snippet auto-tracks the initial page view; Blazor's client-side route
// changes are not browser navigations, so we hook LocationChanged and call
// trackPageView manually.
var nav = host.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
nav.LocationChanged += (sender, args) =>
{
   // Fire-and-forget — TrackPageViewAsync swallows its own exceptions.
   _ = telemetry.TrackPageViewAsync(name: null, uri: args.Location);
};

// ── Unhandled-exception reporting ────────────────────────────────────────────
// Catches anything raised on the WASM main thread that nothing else handles.
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
   if (args.ExceptionObject is Exception ex)
   {
      _ = telemetry.TrackExceptionAsync(ex, new Dictionary<string, string>
        {
            { "source", "AppDomain.UnhandledException" },
            { "isTerminating", args.IsTerminating.ToString() }
        });
   }
};

TaskScheduler.UnobservedTaskException += (sender, args) =>
{
   _ = telemetry.TrackExceptionAsync(args.Exception, new Dictionary<string, string>
    {
        { "source", "TaskScheduler.UnobservedTaskException" }
    });
};

await host.RunAsync();
