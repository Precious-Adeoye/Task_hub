using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using TaskHub.Core.Entities;
using Task_hub.Application.Abstractions;

namespace Task_hub.Application.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IStorage _storage;

        public AuthService(IStorage storage)
        {
            _storage = storage;
        }

        public async Task<AuthResult> RegisterAsync(string username, string email, string password)
        {
            var existingByEmail = await _storage.GetUserByEmailAsync(email);
            var existingByUsername = await _storage.GetUserByUsernameAsync(username);

            if (existingByEmail is not null || existingByUsername is not null)
                return AuthResult.Fail("A user with this email or username already exists.");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            await _storage.AddUserAsync(user);

            return AuthResult.Ok(user);
        }

        public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password)
        {
            // Try email first, then username
            var user = await _storage.GetUserByEmailAsync(usernameOrEmail);
            user ??= await _storage.GetUserByUsernameAsync(usernameOrEmail);

            if (user is null)
                return AuthResult.Fail("Invalid username or password.");

            if (user.IsLocked)
                return AuthResult.Fail("Account is temporarily locked. Please try again later.");

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                }

                await _storage.UpdateUserAsync(user);
                return AuthResult.Fail("Invalid username or password.");
            }

            // Successful login — reset lockout state
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _storage.UpdateUserAsync(user);

            return AuthResult.Ok(user);
        }

        public async Task<User?> GetCurrentUserAsync(Guid userId)
        {
            return await _storage.GetUserByIdAsync(userId);
        }

        public async Task SignInAsync(HttpContext httpContext, User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }
    }
}
