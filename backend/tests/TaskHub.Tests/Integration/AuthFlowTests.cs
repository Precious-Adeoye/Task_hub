using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Api.Dto;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Integration;

public class AuthFlowTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public AuthFlowTests(TaskHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Login_Me_Logout_ShouldWork()
    {
        // Arrange — use cookie-aware client
        var client = _factory.CreateClientWithCookies();

        var uniqueId = Guid.NewGuid().ToString()[..8];
        var username = $"testuser_{uniqueId}";
        var email = $"test_{uniqueId}@example.com";
        var password = "Test123!@#";

        // Act 1: Register
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username,
            email,
            password
        });

        // Assert 1
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerResult.Should().NotBeNull();
        registerResult!.Username.Should().Be(username);

        // Act 2: Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username,
            password
        });

        // Assert 2
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(c => c.Contains("TaskHub.Auth"));

        // Act 3: Get Current User
        var meResponse = await client.GetAsync("/api/v1/auth/me");

        // Assert 3
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var meResult = await meResponse.Content.ReadFromJsonAsync<AuthResponse>();
        meResult!.Username.Should().Be(username);

        // Act 4: Logout
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", null);

        // Assert 4
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 5: Get Current User (should fail)
        var meResponse2 = await client.GetAsync("/api/v1/auth/me");

        // Assert 5
        meResponse2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnGenericError()
    {
        // Arrange
        var client = _factory.CreateClientWithCookies();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "nonexistentuser",
            password = "wrongpassword"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
