using TaskHub.Core.Entities;

namespace Task_hub.Application.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(string username, string email, string password);
        Task<AuthResult> LoginAsync(string usernameOrEmail, string password);
        Task<User?> GetCurrentUserAsync(Guid userId);
    }
}
