using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public static class CalendarProjection
{
   public const string TodayTag = "today";
   public const string PreviousMonthTag = "pre";
   public const string NextMonthTag = "next";

   public static int FindTodayIndex(CalendarData data)
   {
      ArgumentNullException.ThrowIfNull(data);
      return data.Days.FindIndex(day => string.Equals(day.Tag, TodayTag, StringComparison.Ordinal));
   }

   public static CalendarData? TryProject(CalendarData source, DateOnly sourceDate, DateOnly targetDate)
   {
      ArgumentNullException.ThrowIfNull(source);

      int todayIndex = FindTodayIndex(source);
      if (todayIndex < 0)
      {
         return null;
      }

      int targetIndex = todayIndex + (targetDate.DayNumber - sourceDate.DayNumber);
      if (targetIndex == todayIndex)
      {
         return source;
      }

      if (targetIndex < 0 || targetIndex >= source.Days.Count || IsOutsideMonth(source.Days[targetIndex]))
      {
         return null;
      }

      return CopyWithToday(source, targetIndex);
   }

   public static CalendarData WithoutToday(CalendarData source)
   {
      ArgumentNullException.ThrowIfNull(source);
      return CopyWithToday(source, -1);
   }

   private static bool IsOutsideMonth(CalendarDay day) =>
      string.Equals(day.Tag, PreviousMonthTag, StringComparison.Ordinal)
      || string.Equals(day.Tag, NextMonthTag, StringComparison.Ordinal);

   private static CalendarData CopyWithToday(CalendarData source, int todayIndex) => new()
   {
      Name = source.Name,
      Year = source.Year,
      Eng1 = source.Eng1,
      Eng2 = source.Eng2,
      EngYear = source.EngYear,
      WeeksNeFull = [.. source.WeeksNeFull],
      WeeksNeMini = [.. source.WeeksNeMini],
      Days = [.. source.Days.Select((day, index) => new CalendarDay
      {
         Bs = day.Bs,
         Ad = day.Ad,
         Tag = ResolveTag(day.Tag, index == todayIndex)
      })]
   };

   private static string? ResolveTag(string? tag, bool isToday)
   {
      if (isToday)
      {
         return TodayTag;
      }

      return string.Equals(tag, TodayTag, StringComparison.Ordinal) ? null : tag;
   }
}
