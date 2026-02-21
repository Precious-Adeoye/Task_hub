using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Auth;
using TaskHub.Api.Dto;

namespace TaskHub.Api.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for {Email}: {Error}", request.Email, result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            await SignInUserAsync(result.User!);
            _logger.LogInformation("User registered: {Username}", request.Username);

            return Ok(new AuthResponse
            {
                Id = result.User!.Id,
                Username = result.User.Username,
                Email = result.User.Email
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Login failed for {Username}: {Error}", request.Username, result.ErrorMessage);
                return Unauthorized(new { error = result.ErrorMessage });
            }

            await SignInUserAsync(result.User!);
            _logger.LogInformation("User logged in: {Username}", result.User!.Username);

            return Ok(new AuthResponse
            {
                Id = result.User.Id,
                Username = result.User.Username,
                Email = result.User.Email
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User {UserId} logged out",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AuthResponse>> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user is null)
                return Unauthorized();

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }

        private async Task SignInUserAsync(TaskHub.Core.Entities.User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
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
