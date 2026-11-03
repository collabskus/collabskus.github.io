namespace CollabsKus.BlazorWebAssembly.Services;

public interface IApiLogger
{
   void LogApiRequest(string endpoint, object? data, bool fromCache);
}
