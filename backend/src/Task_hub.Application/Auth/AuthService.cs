using TaskHub.Core.Entities;
using Task_hub.Application.Abstraction;

namespace Task_hub.Application.Auth
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
            if (existingByEmail is not null)
                return AuthResult.Fail("A user with this email already exists.");

            var existingByUsername = await _storage.GetUserByUsernameAsync(username);
            if (existingByUsername is not null)
                return AuthResult.Fail("A user with this username already exists.");

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
    }
}
