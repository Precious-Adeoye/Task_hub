using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskHub.Api.Dto;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Concurrency;

public class ConcurrencyTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public ConcurrencyTests(TaskHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentUpdates_ShouldReturn412()
    {
        // Arrange — use cookie-aware client
        var client = _factory.CreateClientWithCookies();

        // Setup: Register and create org
        var uniqueId = Guid.NewGuid().ToString()[..8];
        await SetupUserAndOrg(client, uniqueId);

        // Create a todo
        var createResponse = await client.PostAsJsonAsync("/api/v1/todo", new
        {
            title = "Concurrency Test Todo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        todo.Should().NotBeNull();

        // Get current version
        var getResponse = await client.GetAsync($"/api/v1/todo/{todo!.Id}");
        var etag = getResponse.Headers.ETag?.Tag;

        // Act: Update with correct version (should work)
        var update1Response = await client.PutAsJsonAsync($"/api/v1/todo/{todo.Id}", new
        {
            title = "Updated Title 1"
        });
        update1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Update with stale version (should fail)
        client.DefaultRequestHeaders.Add("If-Match", etag);
        var update2Response = await client.PutAsJsonAsync($"/api/v1/todo/{todo.Id}", new
        {
            title = "Updated Title 2"
        });

        // Assert
        update2Response.StatusCode.Should().Be((HttpStatusCode)412);
    }

    private async Task SetupUserAndOrg(HttpClient client, string uniqueId)
    {
        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"user_{uniqueId}",
            email = $"user_{uniqueId}@test.com",
            password = "Test123!@#"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create org (already authenticated from register)
        var orgResponse = await client.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = $"Test Org {uniqueId}"
        });
        orgResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        org.Should().NotBeNull();
        client.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());
    }
}
