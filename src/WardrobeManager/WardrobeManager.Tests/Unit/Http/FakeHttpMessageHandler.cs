using System.Net;
using System.Net.Http.Json;

namespace WardrobeManager.Tests.Unit.Http;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public int CallCount { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_responder(request));
    }

    public static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = JsonContent.Create(body) };

    public static HttpResponseMessage Status(HttpStatusCode status) => new(status);
}
