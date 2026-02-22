using System.Net;

namespace TaskHub.Tests.Helpers;

public class CookieContainerHandler : DelegatingHandler
{
    private readonly CookieContainer _cookies = new();

    public CookieContainerHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookieHeader = _cookies.GetCookieHeader(request.RequestUri!);
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.Add("Cookie", cookieHeader);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var cookie in setCookieHeaders)
                _cookies.SetCookies(request.RequestUri!, cookie);
        }

        return response;
    }
}
