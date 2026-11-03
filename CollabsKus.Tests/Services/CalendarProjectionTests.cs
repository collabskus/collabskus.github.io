using CollabsKus.BlazorWebAssembly.Services;
using CollabsKus.Tests.Support;

namespace CollabsKus.Tests.Services;

public class CalendarProjectionTests
{
   private static readonly DateOnly SourceDate = new(2026, 4, 20);
   private const int TodayIndex = 10;

   [Test]
   public async Task TryProject_SameDate_ReturnsSameInstance()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate);
      await Assert.That(ReferenceEquals(projected, source)).IsTrue();
   }

   [Test]
   public async Task TryProject_NextDay_MovesTodayForward()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(1));
      await Assert.That(projected is null).IsFalse();
      await Assert.That(CalendarProjection.FindTodayIndex(projected!)).IsEqualTo(TodayIndex + 1);
      await Assert.That(projected!.Days[TodayIndex].Tag is null).IsTrue();
   }

   [Test]
   public async Task TryProject_PreviousDay_MovesTodayBackward()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(-2));
      await Assert.That(CalendarProjection.FindTodayIndex(projected!)).IsEqualTo(TodayIndex - 2);
   }

   [Test]
   public async Task TryProject_DoesNotMutateSource()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(3));
      await Assert.That(CalendarProjection.FindTodayIndex(source)).IsEqualTo(TodayIndex);
   }

   [Test]
   public async Task TryProject_IntoNextMonthDays_ReturnsNull()
   {
      int lastDayIndex = CalendarFixtures.PreviousMonthDays + CalendarFixtures.MonthDays - 1;
      var source = CalendarFixtures.CreateResponse(lastDayIndex).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(1));
      await Assert.That(projected is null).IsTrue();
   }

   [Test]
   public async Task TryProject_IntoPreviousMonthDays_ReturnsNull()
   {
      var source = CalendarFixtures.CreateResponse(CalendarFixtures.PreviousMonthDays).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(-1));
      await Assert.That(projected is null).IsTrue();
   }

   [Test]
   public async Task TryProject_BeyondGrid_ReturnsNull()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(400));
      await Assert.That(projected is null).IsTrue();
   }

   [Test]
   public async Task TryProject_NoTodayTag_ReturnsNull()
   {
      var source = CalendarFixtures.CreateResponse(-1).Res;
      var projected = CalendarProjection.TryProject(source, SourceDate, SourceDate.AddDays(1));
      await Assert.That(projected is null).IsTrue();
   }

   [Test]
   public async Task WithoutToday_RemovesTodayAndKeepsMonthTags()
   {
      var source = CalendarFixtures.CreateResponse(TodayIndex).Res;
      var stale = CalendarProjection.WithoutToday(source);
      await Assert.That(CalendarProjection.FindTodayIndex(stale)).IsEqualTo(-1);
      await Assert.That(stale.Days[0].Tag).IsEqualTo(CalendarProjection.PreviousMonthTag);
      await Assert.That(stale.Days[^1].Tag).IsEqualTo(CalendarProjection.NextMonthTag);
      await Assert.That(stale.Name).IsEqualTo(source.Name);
   }
}
