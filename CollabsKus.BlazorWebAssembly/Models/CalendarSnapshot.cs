namespace CollabsKus.BlazorWebAssembly.Models;

public sealed record CalendarSnapshot(
   CalendarData Data,
   DateOnly ForDate,
   CalendarDataSource Source,
   DateTimeOffset FetchedAt);
