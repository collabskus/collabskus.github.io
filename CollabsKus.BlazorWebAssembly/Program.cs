using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CollabsKus.BlazorWebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Option 1: Register settings as a singleton service
builder.Services.AddSingleton<PortfolioSettings>(sp =>
{
    // You could load this from appsettings.json
    var settings = builder.Configuration.GetSection("Portfolio").Get<PortfolioSettings>()
                   ?? new PortfolioSettings();
    return settings;
});

// Option 2: Create a cascading parameter for the whole app
// This makes the config available to all components

await builder.Build().RunAsync();
