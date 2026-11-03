namespace CollabsKus.Tests.Support;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
   public List<Uri> Requests { get; } = [];

   public static StubHttpMessageHandler Offline() =>
      new(_ => throw new HttpRequestException("Network unavailable"));

   protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
   {
      Requests.Add(request.RequestUri!);
      try
      {
         return Task.FromResult(responder(request));
      }
      catch (Exception ex)
      {
         return Task.FromException<HttpResponseMessage>(ex);
      }
   }
}
