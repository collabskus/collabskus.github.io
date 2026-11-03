using CollabsKus.BlazorWebAssembly;
using CollabsKus.BlazorWebAssembly.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<KathmanduCalendarService>();
builder.Services.AddScoped<MoonPhaseService>();
builder.Services.AddSingleton<SolarPositionService>();
builder.Services.AddScoped<ApiLoggerService>();
builder.Services.AddScoped<BlogService>();

builder.Services.AddSingleton(new TelemetryOptions(
   builder.Configuration[TelemetryOptions.ConnectionStringKey],
   builder.HostEnvironment.Environment));
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
builder.Services.AddSingleton<ILoggerProvider, TelemetryLoggerProvider>();

var host = builder.Build();

var telemetry = host.Services.GetRequiredService<ITelemetryService>();
_ = telemetry.InitializeAsync();

var navigation = host.Services.GetRequiredService<NavigationManager>();
navigation.LocationChanged += (_, e) => _ = telemetry.TrackPageViewAsync(null, e.Location);

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
   if (e.ExceptionObject is Exception ex)
   {
      _ = telemetry.TrackExceptionAsync(ex, new Dictionary<string, string>
      {
         ["source"] = "AppDomain.UnhandledException",
         ["isTerminating"] = e.IsTerminating.ToString()
      });
   }
};

TaskScheduler.UnobservedTaskException += (_, e) =>
{
   _ = telemetry.TrackExceptionAsync(e.Exception, new Dictionary<string, string>
   {
      ["source"] = "TaskScheduler.UnobservedTaskException"
   });
};

await host.RunAsync();
