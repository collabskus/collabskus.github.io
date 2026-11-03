namespace CollabsKus.BlazorWebAssembly.Models;

public sealed record GeoLocation(string Name, double Latitude, double Longitude, TimeSpan UtcOffset);
