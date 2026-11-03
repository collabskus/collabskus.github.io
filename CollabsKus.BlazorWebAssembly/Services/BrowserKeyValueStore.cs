using Microsoft.JSInterop;

namespace CollabsKus.BlazorWebAssembly.Services;

public sealed class BrowserKeyValueStore(IJSRuntime jsRuntime) : IKeyValueStore
{
   public async ValueTask<string?> GetAsync(string key)
   {
      try
      {
         return await jsRuntime.InvokeAsync<string?>("appStorage.get", key);
      }
      catch (JSException)
      {
         return null;
      }
   }

   public async ValueTask<bool> SetAsync(string key, string value)
   {
      try
      {
         return await jsRuntime.InvokeAsync<bool>("appStorage.set", key, value);
      }
      catch (JSException)
      {
         return false;
      }
   }

   public async ValueTask RemoveAsync(string key)
   {
      try
      {
         await jsRuntime.InvokeVoidAsync("appStorage.remove", key);
      }
      catch (JSException)
      {
      }
   }
}
