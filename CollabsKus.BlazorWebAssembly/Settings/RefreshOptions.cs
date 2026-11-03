namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class RefreshOptions : IAppOptions
{
   public static string SectionName => "Refresh";

   public int ClockTickMilliseconds { get; set; }

   public int ClockSyncCheckMinutes { get; set; }

   public int CalendarCheckMinutes { get; set; }

   public TimeSpan ClockTickInterval => TimeSpan.FromMilliseconds(ClockTickMilliseconds);

   public TimeSpan ClockSyncCheckInterval => TimeSpan.FromMinutes(ClockSyncCheckMinutes);

   public TimeSpan CalendarCheckInterval => TimeSpan.FromMinutes(CalendarCheckMinutes);

   public IEnumerable<string> Validate()
   {
      if (ClockTickMilliseconds is < 50 or > 60000)
      {
         yield return "ClockTickMilliseconds must be between 50 and 60000.";
      }

      if (ClockSyncCheckMinutes is < 1 or > 1440)
      {
         yield return "ClockSyncCheckMinutes must be between 1 and 1440.";
      }

      if (CalendarCheckMinutes is < 1 or > 1440)
      {
         yield return "CalendarCheckMinutes must be between 1 and 1440.";
      }
   }
}
