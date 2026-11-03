namespace CollabsKus.BlazorWebAssembly.Settings;

public interface IAppOptions
{
   static abstract string SectionName { get; }

   IEnumerable<string> Validate();
}
