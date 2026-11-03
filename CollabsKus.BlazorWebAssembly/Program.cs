using CollabsKus.BlazorWebAssembly;
using CollabsKus.BlazorWebAssembly.Services;
using CollabsKus.BlazorWebAssembly.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
   .AddAppOptions<LocationOptions>(builder.Configuration)
   .AddAppOptions<CalendarApiOptions>(builder.Configuration)
   .AddAppOptions<RefreshOptions>(builder.Configuration)
   .AddAppOptions<LocalStorageOptions>(builder.Configuration)
   .AddAppOptions<GeolocationOptions>(builder.Configuration)
   .AddAppOptions<ApiLoggerOptions>(builder.Configuration)
   .AddAppOptions<BlogOptions>(builder.Configuration)
   .AddAppOptions<TelemetryOptions>(builder.Configuration);
builder.Services.PostConfigure<TelemetryOptions>(options => options.Environment = builder.HostEnvironment.Environment);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IKeyValueStore, BrowserKeyValueStore>();
builder.Services.AddScoped<ILocalCache, LocalCache>();
builder.Services.AddScoped<IApiLogger, ApiLoggerService>();
builder.Services.AddScoped<IGeolocationService, GeolocationService>();
builder.Services.AddScoped<KathmanduCalendarService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddSingleton<SolarPositionService>();

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
