using Microsoft.Extensions.Logging;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class TelemetryLoggerProvider(ITelemetryService telemetry) : ILoggerProvider
{
   public ILogger CreateLogger(string categoryName) => new TelemetryLogger(categoryName, telemetry);

   public void Dispose()
   {
   }

   private sealed class TelemetryLogger(string categoryName, ITelemetryService telemetry) : ILogger
   {
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

      public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Error and < LogLevel.None;

      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
      {
         if (!IsEnabled(logLevel))
         {
            return;
         }

         var properties = new Dictionary<string, string>
         {
            ["category"] = categoryName,
            ["logLevel"] = logLevel.ToString(),
            ["eventId"] = eventId.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["message"] = formatter(state, exception)
         };

         _ = exception is not null
            ? telemetry.TrackExceptionAsync(exception, properties)
            : telemetry.TrackTraceAsync(properties["message"], ToSeverity(logLevel), properties);
      }

      private static TelemetrySeverity ToSeverity(LogLevel logLevel) => logLevel switch
      {
         LogLevel.Critical => TelemetrySeverity.Critical,
         _ => TelemetrySeverity.Error
      };
   }
}
