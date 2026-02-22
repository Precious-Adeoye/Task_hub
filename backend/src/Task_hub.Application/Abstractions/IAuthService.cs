using Microsoft.AspNetCore.Http;
using TaskHub.Core.Entities;

namespace Task_hub.Application.Abstractions
{
    public interface IAuthService
    {
        Task<Services.AuthResult> RegisterAsync(string username, string email, string password);
        Task<Services.AuthResult> LoginAsync(string usernameOrEmail, string password);
        Task<User?> GetCurrentUserAsync(Guid userId);
        Task SignInAsync(HttpContext httpContext, User user);
    }
}
