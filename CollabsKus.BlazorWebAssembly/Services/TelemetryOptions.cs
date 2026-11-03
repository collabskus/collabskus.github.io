namespace CollabsKus.BlazorWebAssembly.Services;

public sealed record TelemetryOptions(string? ConnectionString, string Environment)
{
   public const string ConnectionStringKey = "ApplicationInsights:ConnectionString";

   public bool IsEnabled => !string.IsNullOrWhiteSpace(ConnectionString);
}
