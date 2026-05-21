using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CollabsKus.BlazorWebAssembly;
using CollabsKus.BlazorWebAssembly.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Cap the global HttpClient timeout. The default (~100s) is far too long for
// any of our calls: blog manifest is a local static file, blog posts are local
// markdown, and the calendar/time/logger endpoints either respond quickly or
// not at all from a UX perspective. A 15s ceiling prevents stuck requests from
// keeping connections alive forever and surfaces real network problems faster.
// Individual call sites that want a shorter bound use their own CancellationToken.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(15)
});

// Register our services
builder.Services.AddScoped<KathmanduCalendarService>();
builder.Services.AddScoped<MoonPhaseService>();
builder.Services.AddSingleton<SolarPositionService>();
builder.Services.AddScoped<ApiLoggerService>();
builder.Services.AddScoped<BlogService>();

await builder.Build().RunAsync();
