using CollabsKus.BlazorWebAssembly.Services;
using Microsoft.Extensions.Logging;

namespace CollabsKus.Tests.Services;

public class TelemetryLoggerProviderTests
{
   [Test]
   [Arguments(LogLevel.Trace)]
   [Arguments(LogLevel.Debug)]
   [Arguments(LogLevel.Information)]
   [Arguments(LogLevel.Warning)]
   [Arguments(LogLevel.None)]
   public async Task IsEnabled_BelowError_ReturnsFalse(LogLevel level)
   {
      var logger = new TelemetryLoggerProvider(new RecordingTelemetryService()).CreateLogger("Test");
      await Assert.That(logger.IsEnabled(level)).IsFalse();
   }

   [Test]
   [Arguments(LogLevel.Error)]
   [Arguments(LogLevel.Critical)]
   public async Task IsEnabled_ErrorOrAbove_ReturnsTrue(LogLevel level)
   {
      var logger = new TelemetryLoggerProvider(new RecordingTelemetryService()).CreateLogger("Test");
      await Assert.That(logger.IsEnabled(level)).IsTrue();
   }

   [Test]
   public async Task Log_WarningLevel_SendsNothing()
   {
      var telemetry = new RecordingTelemetryService();
      var logger = new TelemetryLoggerProvider(telemetry).CreateLogger("Test");

      logger.LogWarning("ignored");

      await Assert.That(telemetry.Exceptions.Count).IsEqualTo(0);
      await Assert.That(telemetry.Traces.Count).IsEqualTo(0);
   }

   [Test]
   public async Task Log_ErrorWithException_TracksException()
   {
      var telemetry = new RecordingTelemetryService();
      var logger = new TelemetryLoggerProvider(telemetry).CreateLogger("Some.Category");
      var exception = new InvalidOperationException("boom");

      logger.LogError(exception, "failed");

      await Assert.That(telemetry.Exceptions.Count).IsEqualTo(1);
      await Assert.That(telemetry.Traces.Count).IsEqualTo(0);
      await Assert.That(ReferenceEquals(telemetry.Exceptions[0].Exception, exception)).IsTrue();
      await Assert.That(telemetry.Exceptions[0].Properties!["category"]).IsEqualTo("Some.Category");
      await Assert.That(telemetry.Exceptions[0].Properties!["logLevel"]).IsEqualTo("Error");
      await Assert.That(telemetry.Exceptions[0].Properties!["message"]).IsEqualTo("failed");
   }

   [Test]
   public async Task Log_CriticalWithoutException_TracksCriticalTrace()
   {
      var telemetry = new RecordingTelemetryService();
      var logger = new TelemetryLoggerProvider(telemetry).CreateLogger("Test");

      logger.LogCritical("fatal");

      await Assert.That(telemetry.Exceptions.Count).IsEqualTo(0);
      await Assert.That(telemetry.Traces.Count).IsEqualTo(1);
      await Assert.That(telemetry.Traces[0].Message).IsEqualTo("fatal");
      await Assert.That(telemetry.Traces[0].Severity).IsEqualTo(TelemetrySeverity.Critical);
   }

   [Test]
   public async Task Log_ErrorWithoutException_TracksErrorTrace()
   {
      var telemetry = new RecordingTelemetryService();
      var logger = new TelemetryLoggerProvider(telemetry).CreateLogger("Test");

      logger.LogError("bad");

      await Assert.That(telemetry.Traces.Count).IsEqualTo(1);
      await Assert.That(telemetry.Traces[0].Severity).IsEqualTo(TelemetrySeverity.Error);
   }

   private sealed class RecordingTelemetryService : ITelemetryService
   {
      public List<(Exception Exception, IReadOnlyDictionary<string, string>? Properties)> Exceptions { get; } = [];

      public List<(string Message, TelemetrySeverity Severity)> Traces { get; } = [];

      public Task InitializeAsync() => Task.CompletedTask;

      public Task TrackPageViewAsync(string? name, string? uri) => Task.CompletedTask;

      public Task TrackEventAsync(string name, IReadOnlyDictionary<string, string>? properties = null) => Task.CompletedTask;

      public Task TrackExceptionAsync(Exception exception, IReadOnlyDictionary<string, string>? properties = null)
      {
         Exceptions.Add((exception, properties));
         return Task.CompletedTask;
      }

      public Task TrackTraceAsync(string message, TelemetrySeverity severity = TelemetrySeverity.Information, IReadOnlyDictionary<string, string>? properties = null)
      {
         Traces.Add((message, severity));
         return Task.CompletedTask;
      }

      public Task FlushAsync() => Task.CompletedTask;
   }
}
