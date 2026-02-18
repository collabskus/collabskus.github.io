namespace CollabsKus.BlazorWebAssembly.Models;

public class MoonPhase
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public double Illumination { get; set; }
    public double Age { get; set; }

    // Days until / since key events
    public double DaysUntilNewMoon { get; set; }
    public double DaysUntilFullMoon { get; set; }
    public double DaysSinceNewMoon { get; set; }
    public double DaysSinceFullMoon { get; set; }

    // Nepal Sambat / Hindu lunar calendar concepts
    // TithiNumber: 1–15 in each paksha
    public int TithiNumber { get; set; }
    // TithiName: Nepali/Sanskrit name e.g. "Pratipada", "Purnima", "Amavasya"
    public string TithiName { get; set; } = string.Empty;
    // Paksha: "Shukla" (waxing/bright) or "Krishna" (waning/dark)
    public string Paksha { get; set; } = string.Empty;
}
