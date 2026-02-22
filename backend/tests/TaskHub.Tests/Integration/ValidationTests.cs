using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskHub.Api.Dto;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Integration;

public class ValidationTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public ValidationTests(TaskHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturn400()
    {
        var client = _factory.CreateClientWithCookies();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "testuser",
            email = "test@example.com",
            password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400()
    {
        var client = _factory.CreateClientWithCookies();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = "testuser",
            email = "not-an-email",
            password = "Test123!@#"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTodo_WithEmptyTitle_ShouldReturn400()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/todo", new
        {
            title = "",
            priority = "High"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTodo_WithTitleTooShort_ShouldReturn400()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/todo", new
        {
            title = "ab"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrganisation_WithShortName_ShouldReturn400()
    {
        var client = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"user_{uniqueId}",
            email = $"user_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        var response = await client.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = "ab"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"user_{uniqueId}",
            email = $"user_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        var orgResponse = await client.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = $"Org_{uniqueId}"
        });
        var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        client.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());

        return client;
    }
}
