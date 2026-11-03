using System.Diagnostics;
using System.Text.Json;

namespace CollabsKus.BlazorWebAssembly.Services;

public class ApiLoggerService(HttpClient httpClient)
{
   private const string LoggerUrl = "https://my-api.2w7sp317.workers.dev/ui/notes/create";
   private const string VaultUrl = "https://my-api.2w7sp317.workers.dev/api/vault";
   private const string PageUrl = "https://collabskus.github.io";
   private const string UserAgentLabel = "Blazor WebAssembly";
   private const int MaxContentLength = 1000;

   private static readonly JsonSerializerOptions DefaultOptions = new()
   {
      WriteIndented = true
   };

   public static JsonSerializerOptions GetOptions() => DefaultOptions;

   /// <summary>
   /// Builds the log payload synchronously, then posts it to both endpoints
   /// in the background. Returns immediately; failures are non-fatal.
   /// </summary>
   public Task LogApiRequestAsync(string endpoint, object data, bool fromCache, JsonSerializerOptions options)
   {
      try
      {
         var logContent = JsonSerializer.Serialize(new
         {
            endpoint,
            timestamp = DateTime.UtcNow.ToString("O"),
            fromCache,
            data,
            userAgent = UserAgentLabel,
            page = PageUrl
         }, options);

         var title = $"API Request: {endpoint}";
         var notesContent = Truncate(logContent, MaxContentLength);
         var vaultContent = Truncate($"{title}\n{logContent}", MaxContentLength);

         // Independent fire-and-forget posts: one failing must not block the other.
         _ = PostFormAsync(LoggerUrl, new Dictionary<string, string>
         {
            ["title"] = title,
            ["content"] = notesContent
         });

         _ = PostFormAsync(VaultUrl, new Dictionary<string, string>
         {
            ["content"] = vaultContent
         });
      }
      catch (Exception ex)
      {
         // Serialization failed; logging is non-critical.
         Debug.WriteLine($"ApiLoggerService: failed to build log payload: {ex.Message}");
      }

      return Task.CompletedTask;
   }

   private async Task PostFormAsync(string url, Dictionary<string, string> formData)
   {
      try
      {
         using var request = new HttpRequestMessage(HttpMethod.Post, url)
         {
            Content = new FormUrlEncodedContent(formData)
         };
         request.Headers.Add("Accept", "application/json");

         using var response = await httpClient.SendAsync(request);

         if (!response.IsSuccessStatusCode)
         {
            Debug.WriteLine($"ApiLoggerService: {url} returned {(int)response.StatusCode}");
         }
      }
      catch (Exception ex)
      {
         // Logging is non-critical, but leave a trace for debugging.
         Debug.WriteLine($"ApiLoggerService: post to {url} failed: {ex.Message}");
      }
   }

   private static string Truncate(string value, int maxLength)
   {
      if (value.Length <= maxLength)
      {
         return value;
      }

      const string suffix = "...";
      var cut = maxLength - suffix.Length;

      // Don't split a surrogate pair.
      if (cut > 0 && char.IsHighSurrogate(value[cut - 1]))
      {
         cut--;
      }

      return string.Concat(value.AsSpan(0, cut), suffix);
   }
}
