using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace TaskHub.Tests.Helpers;

public class TaskHubWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Relax cookie secure policy for test environment (test server uses HTTP)
            services.PostConfigure<CookieAuthenticationOptions>(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });
        });
    }

    /// <summary>
    /// Creates an HttpClient that automatically handles cookies between requests.
    /// </summary>
    public HttpClient CreateClientWithCookies()
    {
        var handler = new CookieContainerHandler(Server.CreateHandler());
        var client = new HttpClient(handler) { BaseAddress = Server.BaseAddress };
        return client;
    }
}
