namespace CollabsKus.BlazorWebAssembly.Models;

public class SolarPosition
{
   public const double GoldenHourElevationDegrees = 6.0;

   public string LocationName { get; init; } = "";
   public double Latitude { get; init; }
   public double Longitude { get; init; }

   public DateTime LocalTime { get; init; }

   public double Altitude { get; init; }

   public double Azimuth { get; init; }

   public double Declination { get; init; }

   public double HourAngle { get; init; }

   public bool IsAboveHorizon => Altitude > 0;

   public bool IsGoldenHour => Altitude is > -GoldenHourElevationDegrees and < GoldenHourElevationDegrees;

   public double DayFraction { get; init; }

   public double FullDayFraction { get; init; }

   public TimeOnly? SunriseLocal { get; init; }

   public TimeOnly SolarNoonLocal { get; init; }

   public TimeOnly? SunsetLocal { get; init; }

   public DateTime? PreviousSunrise { get; init; }

   public DateTime? PreviousSunset { get; init; }

   public DateTime? NextSunrise { get; init; }

   public DateTime? NextSunset { get; init; }

   public TimeOnly? GoldenHourMorningStart { get; init; }

   public TimeOnly? GoldenHourMorningEnd { get; init; }

   public TimeOnly? GoldenHourEveningStart { get; init; }

   public TimeOnly? GoldenHourEveningEnd { get; init; }

   public double MaxElevation { get; init; }

   public TimeSpan DayLength { get; init; }

   public string AzimuthCardinal => AzToCardinal(Azimuth);
   public string AltitudeSign => Altitude >= 0 ? "+" : "";

   private static string AzToCardinal(double az)
   {
      string[] dirs = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
      return dirs[(int)Math.Round(az / 22.5) % 16];
   }
}
