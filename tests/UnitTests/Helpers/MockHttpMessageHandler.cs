namespace UnitTests.Helpers;

using System.Net;
using System.Text;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public void EnqueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

    public void EnqueueResponse(string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }

    public List<HttpRequestMessage> SentRequests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        if (_responses.Count > 0)
            return Task.FromResult(_responses.Dequeue());
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }
}

/// <summary>
/// A mock handler that introduces a delay before responding, useful for timeout tests.
/// </summary>
public class SlowHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;
    private readonly HttpResponseMessage _response;

    public SlowHttpMessageHandler(TimeSpan delay, HttpResponseMessage? response = null)
    {
        _delay = delay;
        _response = response ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        return _response;
    }
}
