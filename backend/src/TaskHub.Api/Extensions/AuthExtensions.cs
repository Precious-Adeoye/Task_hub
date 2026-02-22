using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Task_hub.Application.Authorization;

namespace TaskHub.Api.Extensions;

public static class AuthExtensions
{
    /// <summary>
    /// CSRF Strategy:
    /// Cookie-based auth uses SameSite=Strict which prevents the browser from sending
    /// the cookie on any cross-site request (navigation or AJAX). Combined with the
    /// same-origin frontend policy (CORS restricted to localhost:3000), this provides
    /// robust CSRF protection without a separate anti-forgery token.
    /// See: https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // HttpOnly: prevents JavaScript access to the cookie (mitigates XSS cookie theft)
                options.Cookie.HttpOnly = true;
                // Secure=Always: cookie only sent over HTTPS (prevents sniffing)
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // SameSite=Strict: cookie never sent on cross-site requests (CSRF protection)
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Name = "TaskHub.Auth";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("OrgMember", policy =>
                policy.Requirements.Add(new OrganisationRequirement(requireAdmin: false)));

            options.AddPolicy("OrgAdmin", policy =>
                policy.Requirements.Add(new OrganisationRequirement(requireAdmin: true)));
        });

        services.AddScoped<IAuthorizationHandler, OrganisationAuthorizationHandler>();

        return services;
    }
}
