using CollabsKus.BlazorWebAssembly.Models;
using CollabsKus.BlazorWebAssembly.Services;

namespace CollabsKus.Tests.Support;

internal static class CalendarFixtures
{
   public const int PreviousMonthDays = 3;
   public const int MonthDays = 30;
   public const int NextMonthDays = 2;

   public static CalendarResponse CreateResponse(int todayIndex)
   {
      var days = new List<CalendarDay>();

      for (int i = 0; i < PreviousMonthDays; i++)
      {
         days.Add(new CalendarDay { Bs = $"{29 + i}", Ad = $"{10 + i}", Tag = CalendarProjection.PreviousMonthTag });
      }

      for (int i = 0; i < MonthDays; i++)
      {
         int index = PreviousMonthDays + i;
         days.Add(new CalendarDay { Bs = $"{i + 1}", Ad = $"{13 + i}", Tag = index == todayIndex ? CalendarProjection.TodayTag : null });
      }

      for (int i = 0; i < NextMonthDays; i++)
      {
         days.Add(new CalendarDay { Bs = $"{i + 1}", Ad = $"{i + 1}", Tag = CalendarProjection.NextMonthTag });
      }

      return new CalendarResponse
      {
         Message = "ok",
         Res = new CalendarData
         {
            Name = "Baisakh",
            Year = "2083",
            Eng1 = "Apr",
            Eng2 = "May",
            EngYear = 2026,
            WeeksNeFull = ["आइतबार", "सोमबार", "मंगलबार", "बुधबार", "बिहिबार", "शुक्रबार", "शनिबार"],
            WeeksNeMini = ["आ", "सो", "मं", "बु", "बि", "शु", "श"],
            Days = days
         }
      };
   }
}
