using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Extensions;
using Task_hub.Application.Services;
using TaskHub.Api.Dto;

namespace TaskHub.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditService _auditService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IAuditService auditService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Registration failed for {Email}: {Error}", request.Email, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "Registration failed",
                    Detail = result.ErrorMessage,
                    Status = 400,
                    Instance = Request.Path
                });
            }

            await _authService.SignInAsync(HttpContext, result.User!);
            _logger.LogInformation("User registered: {Username}", request.Username);

            return Ok(new AuthResponse
            {
                Id = result.User!.Id,
                Username = result.User.Username,
                Email = result.User.Email
            });
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);

            if (!result.Success)
            {
                _logger.LogWarning("Login failed for {Username}: {Error}", request.Username, result.ErrorMessage);

                await _auditService.AuditAsync("LoginFailed", "User", request.Username,
                    "Login attempt failed", Guid.Empty, Guid.Empty);

                return Unauthorized(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Authentication failed",
                    Detail = result.ErrorMessage,
                    Status = 401,
                    Instance = Request.Path
                });
            }

            await _authService.SignInAsync(HttpContext, result.User!);
            _logger.LogInformation("User logged in: {Username}", result.User!.Username);

            await _auditService.AuditAsync("LoginSuccess", "User", result.User.Id.ToString(),
                $"User '{result.User.Username}' logged in", result.User.Id, Guid.Empty);

            return Ok(new AuthResponse
            {
                Id = result.User.Id,
                Username = result.User.Username,
                Email = result.User.Email
            });
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userId = User.GetUserId();

            _logger.LogInformation("User {UserId} logged out", userId);

            if (userId.HasValue)
            {
                await _auditService.AuditAsync("Logout", "User", userId.Value.ToString(),
                    "User logged out", userId.Value, Guid.Empty);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Me()
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized();

            var user = await _authService.GetCurrentUserAsync(userId.Value);
            if (user is null)
                return Unauthorized();

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}
