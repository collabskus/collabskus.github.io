using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

/// <summary>
/// Calculates sun position and daily solar events for any location using the
/// Jean Meeus astronomical algorithm (Astronomical Algorithms, 2nd ed.).
/// Pure math — no network calls, no JS interop.
///
/// NOTE: Sunrise/sunset times are "astronomical" — they assume a perfectly flat,
/// unobstructed horizon with standard atmospheric refraction (zenith 90.833°).
/// They do NOT account for local elevation, surrounding hills, or terrain.
/// In valleys like Kathmandu, actual visible sunrise may be later and sunset
/// earlier than the times calculated here.
/// </summary>
public class SolarPositionService
{
    // Kathmandu coordinates
    public const double KathmanduLat = 27.6984037;
    public const double KathmanduLng = 85.2939889;

    // Nepal Standard Time = UTC + 5h 45m
    public static readonly TimeSpan NstOffset = TimeSpan.FromMinutes(5 * 60 + 45);

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Convenience method: compute SolarPosition for Kathmandu.
    /// </summary>
    public SolarPosition CalculateKathmandu(DateTime utcNow)
    {
        return Calculate(utcNow, KathmanduLat, KathmanduLng, NstOffset, "Kathmandu, Nepal");
    }

    /// <summary>
    /// Compute the full SolarPosition snapshot for a given location and UTC instant.
    /// </summary>
    public SolarPosition Calculate(DateTime utcNow, double lat, double lng, TimeSpan tzOffset, string locationName)
    {
        var (alt, az, decl, ha) = GetAltAz(utcNow, lat, lng);
        var events = GetDailyEvents(utcNow, lat, lng);

        double dayFraction = -1;
        if (events.SunriseUtcMinutes.HasValue && events.SunsetUtcMinutes.HasValue)
        {
            double totalDayMin = events.SunsetUtcMinutes.Value - events.SunriseUtcMinutes.Value;
            double utcMinutes = utcNow.Hour * 60.0 + utcNow.Minute + utcNow.Second / 60.0;
            dayFraction = Math.Clamp((utcMinutes - events.SunriseUtcMinutes.Value) / totalDayMin, 0, 1);
        }

        // Full 24-hour day fraction: 0 = solar midnight, 0.5 = solar noon
        double utcMin = utcNow.Hour * 60.0 + utcNow.Minute + utcNow.Second / 60.0;
        double solarMidnightUtc = events.SolarNoonUtcMinutes - 720.0;
        double fullDayFrac = (utcMin - solarMidnightUtc) / 1440.0;
        fullDayFrac -= Math.Floor(fullDayFrac); // normalize to [0, 1)

        var (prevSr, prevSs, nextSr, nextSs) = ComputePrevNext(utcNow, lat, lng, tzOffset);

        return new SolarPosition
        {
            LocationName = locationName,
            Latitude = lat,
            Longitude = lng,
            Altitude = alt,
            Azimuth = az,
            Declination = decl,
            HourAngle = ha,
            DayFraction = dayFraction,
            FullDayFraction = fullDayFrac,
            SunriseLocal = UtcMinutesToLocal(events.SunriseUtcMinutes, tzOffset),
            SolarNoonLocal = UtcMinutesToLocal(events.SolarNoonUtcMinutes, tzOffset)!.Value,
            SunsetLocal = UtcMinutesToLocal(events.SunsetUtcMinutes, tzOffset),
            PreviousSunrise = prevSr,
            PreviousSunset = prevSs,
            NextSunrise = nextSr,
            NextSunset = nextSs,
            GoldenHourMorningStart = UtcMinutesToLocal(events.GoldenMorningStartUtcMin, tzOffset),
            GoldenHourMorningEnd = UtcMinutesToLocal(events.GoldenMorningEndUtcMin, tzOffset),
            GoldenHourEveningStart = UtcMinutesToLocal(events.GoldenEveningStartUtcMin, tzOffset),
            GoldenHourEveningEnd = UtcMinutesToLocal(events.GoldenEveningEndUtcMin, tzOffset),
            MaxElevation = events.MaxElevation,
            DayLength = events.SunriseUtcMinutes.HasValue && events.SunsetUtcMinutes.HasValue
                ? TimeSpan.FromMinutes(events.SunsetUtcMinutes.Value - events.SunriseUtcMinutes.Value)
                : TimeSpan.Zero,
        };
    }

    // ── Previous / Next sunrise & sunset ──────────────────────────────────

    private static (DateTime? prevSunrise, DateTime? prevSunset, DateTime? nextSunrise, DateTime? nextSunset)
        ComputePrevNext(DateTime utcNow, double lat, double lng, TimeSpan tzOffset)
    {
        var localNow = utcNow + tzOffset;

        var dates = new[] { utcNow.Date.AddDays(-1), utcNow.Date, utcNow.Date.AddDays(1) };
        var allSunrises = new List<DateTime>();
        var allSunsets = new List<DateTime>();

        foreach (var date in dates)
        {
            var ev = GetDailyEvents(new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc), lat, lng);
            if (ev.SunriseUtcMinutes.HasValue)
            {
                allSunrises.Add(date.AddMinutes(ev.SunriseUtcMinutes.Value) + tzOffset);
            }
            if (ev.SunsetUtcMinutes.HasValue)
            {
                allSunsets.Add(date.AddMinutes(ev.SunsetUtcMinutes.Value) + tzOffset);
            }
        }

        var prevSrList = allSunrises.Where(s => s <= localNow).OrderByDescending(s => s).ToList();
        var nextSrList = allSunrises.Where(s => s > localNow).OrderBy(s => s).ToList();
        var prevSsList = allSunsets.Where(s => s <= localNow).OrderByDescending(s => s).ToList();
        var nextSsList = allSunsets.Where(s => s > localNow).OrderBy(s => s).ToList();

        return (
            prevSrList.Count > 0 ? prevSrList[0] : null,
            prevSsList.Count > 0 ? prevSsList[0] : null,
            nextSrList.Count > 0 ? nextSrList[0] : null,
            nextSsList.Count > 0 ? nextSsList[0] : null
        );
    }

    // ── Core algorithm ────────────────────────────────────────────────────

    internal static (double alt, double az, double decl, double ha) GetAltAz(DateTime utc, double lat, double lng)
    {
        double jd = ToJulianDay(utc);
        double T = (jd - 2451545.0) / 36525.0;

        double L0 = NormDeg(280.46646 + T * (36000.76983 + T * 0.0003032));
        double M = NormDeg(357.52911 + T * (35999.05029 - 0.0001537 * T));
        double Mr = ToRad(M);
        double C = (1.914602 - T * (0.004817 + 0.000014 * T)) * Math.Sin(Mr)
                     + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mr)
                     + 0.000289 * Math.Sin(3 * Mr);
        double sunLon = L0 + C;
        double omega = 125.04 - 1934.136 * T;
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(ToRad(omega));
        double eps0 = 23.0 + (26.0 + (21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813))) / 60.0) / 60.0;
        double epsilon = eps0 + 0.00256 * Math.Cos(ToRad(omega));

        double decl = ToDeg(Math.Asin(Math.Sin(ToRad(epsilon)) * Math.Sin(ToRad(lambda))));
        double RA = NormDeg(ToDeg(Math.Atan2(
                            Math.Cos(ToRad(epsilon)) * Math.Sin(ToRad(lambda)),
                            Math.Cos(ToRad(lambda)))));

        double GMST = NormDeg(280.46061837 + 360.98564736629 * (jd - 2451545.0)
                        + T * T * (0.000387933 - T / 38710000.0));
        double LST = NormDeg(GMST + lng);
        double ha = NormDeg(LST - RA);
        if (ha > 180) ha -= 360;
        double HAr = ToRad(ha);
        double latr = ToRad(lat);
        double declr = ToRad(decl);

        double alt = ToDeg(Math.Asin(
            Math.Sin(latr) * Math.Sin(declr) + Math.Cos(latr) * Math.Cos(declr) * Math.Cos(HAr)));
        double az = NormDeg(ToDeg(Math.Atan2(
            -Math.Sin(HAr),
            Math.Tan(declr) * Math.Cos(latr) - Math.Sin(latr) * Math.Cos(HAr))));

        return (alt, az, decl, ha);
    }

    // ── Daily events (sunrise, sunset, golden hour) ───────────────────────

    internal record DailyEvents(
        double? SunriseUtcMinutes,
        double SolarNoonUtcMinutes,
        double? SunsetUtcMinutes,
        double MaxElevation,
        double? GoldenMorningStartUtcMin,
        double? GoldenMorningEndUtcMin,
        double? GoldenEveningStartUtcMin,
        double? GoldenEveningEndUtcMin
    );

    internal static DailyEvents GetDailyEvents(DateTime utc, double lat, double lng)
    {
        double jd = ToJulianDay(new DateTime(utc.Year, utc.Month, utc.Day, 12, 0, 0, DateTimeKind.Utc));
        double T = (jd - 2451545.0) / 36525.0;

        double L0 = NormDeg(280.46646 + T * (36000.76983 + T * 0.0003032));
        double M = NormDeg(357.52911 + T * (35999.05029 - 0.0001537 * T));
        double Mr = ToRad(M);
        double C = (1.914602 - T * (0.004817 + 0.000014 * T)) * Math.Sin(Mr)
                      + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mr)
                      + 0.000289 * Math.Sin(3 * Mr);
        double sunLon = L0 + C;
        double omega = 125.04 - 1934.136 * T;
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(ToRad(omega));
        double eps0 = 23.0 + (26.0 + (21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813))) / 60.0) / 60.0;
        double epsilon = eps0 + 0.00256 * Math.Cos(ToRad(omega));
        double decl = ToDeg(Math.Asin(Math.Sin(ToRad(epsilon)) * Math.Sin(ToRad(lambda))));

        // Earth's orbital eccentricity (Meeus eq. 25.4)
        double e = 0.016708634 - T * (0.000042037 + 0.0000001267 * T);

        // Equation of time (minutes) — Meeus Ch. 28
        double y = Math.Pow(Math.Tan(ToRad(epsilon / 2)), 2);
        double eot = 4.0 * ToDeg(
            y * Math.Sin(2 * ToRad(L0))
            - 2.0 * e * Math.Sin(Mr)
            + 4.0 * e * y * Math.Sin(Mr) * Math.Cos(2 * ToRad(L0))
            - 0.5 * y * y * Math.Sin(4 * ToRad(L0))
            - 1.25 * e * e * Math.Sin(2 * Mr));

        double solarNoonUtc = 720.0 - 4.0 * lng - eot;

        double cosHA = (Math.Cos(ToRad(90.833)) - Math.Sin(ToRad(lat)) * Math.Sin(ToRad(decl)))
                     / (Math.Cos(ToRad(lat)) * Math.Cos(ToRad(decl)));

        double? sunriseMin = null, sunsetMin = null;
        if (cosHA is >= -1 and <= 1)
        {
            double hasr = ToDeg(Math.Acos(cosHA));
            sunriseMin = solarNoonUtc - 4.0 * hasr;
            sunsetMin = solarNoonUtc + 4.0 * hasr;
        }

        double maxElev = 90.0 - Math.Abs(lat - decl);

        static double? HaMinutes(double elevDeg, double latDeg, double declDeg)
        {
            double cosH = (Math.Sin(ToRad(elevDeg)) - Math.Sin(ToRad(latDeg)) * Math.Sin(ToRad(declDeg)))
                        / (Math.Cos(ToRad(latDeg)) * Math.Cos(ToRad(declDeg)));
            if (cosH is < -1 or > 1) return null;
            return ToDeg(Math.Acos(cosH)) * 4.0;
        }

        double? ha6 = HaMinutes(6, lat, decl);
        double? ha_6 = HaMinutes(-6, lat, decl);

        return new DailyEvents(
            SunriseUtcMinutes: sunriseMin,
            SolarNoonUtcMinutes: solarNoonUtc,
            SunsetUtcMinutes: sunsetMin,
            MaxElevation: maxElev,
            GoldenMorningStartUtcMin: ha_6.HasValue ? solarNoonUtc - ha_6.Value : null,
            GoldenMorningEndUtcMin: ha6.HasValue ? solarNoonUtc - ha6.Value : null,
            GoldenEveningStartUtcMin: ha6.HasValue ? solarNoonUtc + ha6.Value : null,
            GoldenEveningEndUtcMin: ha_6.HasValue ? solarNoonUtc + ha_6.Value : null
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static TimeOnly? UtcMinutesToLocal(double? utcMinutes, TimeSpan tzOffset)
    {
        if (!utcMinutes.HasValue) return null;
        double localMin = (utcMinutes.Value + tzOffset.TotalMinutes) % 1440;
        if (localMin < 0) localMin += 1440;
        int h = (int)(localMin / 60) % 24;
        int m = (int)(localMin % 60);
        int s = (int)((localMin % 1) * 60);
        return new TimeOnly(h, m, s);
    }

    internal static double ToJulianDay(DateTime utc)
        => utc.ToOADate() + 2415018.5;

    internal static double ToRad(double deg) => deg * Math.PI / 180.0;
    internal static double ToDeg(double rad) => rad * 180.0 / Math.PI;
    internal static double NormDeg(double d) => ((d % 360) + 360) % 360;
}
