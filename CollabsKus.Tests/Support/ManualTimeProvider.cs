namespace CollabsKus.Tests.Support;

internal sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
   public DateTimeOffset UtcNow { get; set; } = utcNow;

   public override DateTimeOffset GetUtcNow() => UtcNow;

   public void Advance(TimeSpan duration) => UtcNow += duration;
}
