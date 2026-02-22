using TaskHub.Core.Entities;

namespace Task_hub.Application.Services
{
    public class AuthResult
    {
        public bool Success { get; private set; }
        public User? User { get; private set; }
        public string? ErrorMessage { get; private set; }

        private AuthResult() { }

        public static AuthResult Ok(User user) => new()
        {
            Success = true,
            User = user
        };

        public static AuthResult Fail(string errorMessage) => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
