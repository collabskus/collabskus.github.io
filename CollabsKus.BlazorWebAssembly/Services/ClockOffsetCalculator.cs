using System.Globalization;
using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public static class ClockOffsetCalculator
{
   private static readonly TimeSpan HalfDay = TimeSpan.FromHours(12);
   private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

   public static TimeSpan? Calculate(TimeResponse response, DateTime requestStartUtc, DateTime responseUtc, TimeSpan expectedOffset)
   {
      ArgumentNullException.ThrowIfNull(response);

      if (!TryParseTimeOfDay(response, out TimeSpan serverTimeOfDay))
      {
         return null;
      }

      DateTime midpointUtc = requestStartUtc + (responseUtc - requestStartUtc) / 2;
      TimeSpan offset = midpointUtc.Date + serverTimeOfDay - midpointUtc;
      return Normalize(offset, expectedOffset);
   }

   public static TimeSpan Normalize(TimeSpan offset, TimeSpan expectedOffset)
   {
      while (offset - expectedOffset > HalfDay)
      {
         offset -= OneDay;
      }

      while (offset - expectedOffset < -HalfDay)
      {
         offset += OneDay;
      }

      return offset;
   }

   private static bool TryParseTimeOfDay(TimeResponse response, out TimeSpan timeOfDay)
   {
      timeOfDay = default;

      if (!TryParseComponent(response.Hour, 23, out int hour)
          || !TryParseComponent(response.Min, 59, out int minute)
          || !TryParseComponent(response.Sec, 59, out int second))
      {
         return false;
      }

      if (hour <= 12)
      {
         bool isPm = string.Equals(response.ApOrPm?.Trim(), "PM", StringComparison.OrdinalIgnoreCase);
         hour %= 12;
         if (isPm)
         {
            hour += 12;
         }
      }

      timeOfDay = new TimeSpan(hour, minute, second);
      return true;
   }

   private static bool TryParseComponent(string? value, int max, out int result) =>
      int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
      && result >= 0
      && result <= max;
}
