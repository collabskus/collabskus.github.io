namespace CollabsKus.BlazorWebAssembly.Settings;

public sealed class LocalStorageOptions : IAppOptions
{
   public static string SectionName => "LocalStorage";

   public string KeyPrefix { get; set; } = string.Empty;

   public int SchemaVersion { get; set; }

   public IEnumerable<string> Validate()
   {
      if (string.IsNullOrWhiteSpace(KeyPrefix))
      {
         yield return "KeyPrefix is required.";
      }

      if (SchemaVersion < 1)
      {
         yield return "SchemaVersion must be at least 1.";
      }
   }
}
