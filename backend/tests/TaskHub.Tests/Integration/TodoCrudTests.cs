using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskHub.Api.Dto;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Integration;

public class TodoCrudTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public TodoCrudTests(TaskHubWebApplicationFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task CreateTodo_ShouldReturn201WithTodo()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/todo", new
        {
            title = "Test Todo",
            description = "A test description",
            priority = "High",
            tags = new[] { "test", "integration" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("Test Todo");
        todo.Description.Should().Be("A test description");
        todo.Priority.Should().Be("High");
        todo.Tags.Should().Contain("test");
        todo.Status.Should().Be("Open");
        todo.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTodo_ShouldReturnTodoWithETag()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/todo", new { title = "ETag Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        var getResponse = await client.GetAsync($"/api/v1/todo/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Headers.ETag.Should().NotBeNull();
        var todo = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        todo!.Title.Should().Be("ETag Test");
    }

    [Fact]
    public async Task GetTodos_ShouldReturnPaginatedList()
    {
        var client = await CreateAuthenticatedClient();

        // Create 3 todos
        for (int i = 1; i <= 3; i++)
        {
            await client.PostAsJsonAsync("/api/v1/todo", new { title = $"List Todo {i}" });
        }

        var response = await client.GetAsync("/api/v1/todo?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        todos.Should().NotBeNull();
        todos!.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateTodo_ShouldModifyFields()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/todo", new { title = "Original Title" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/todo/{created!.Id}", new
        {
            title = "Updated Title",
            description = "New description"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>();
        updated!.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("New description");
    }

    [Fact]
    public async Task ToggleTodo_ShouldSwitchStatus()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/todo", new { title = "Toggle Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        created!.Status.Should().Be("Open");

        // Toggle to Done
        var toggleResponse = await client.PatchAsync($"/api/v1/todo/{created.Id}/toggle", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await toggleResponse.Content.ReadFromJsonAsync<TodoResponse>();
        toggled!.Status.Should().Be("Done");

        // Toggle back to Open
        var toggleBack = await client.PatchAsync($"/api/v1/todo/{created.Id}/toggle", null);
        var toggledBack = await toggleBack.Content.ReadFromJsonAsync<TodoResponse>();
        toggledBack!.Status.Should().Be("Open");
    }

    [Fact]
    public async Task SoftDelete_Restore_HardDelete_Flow()
    {
        var client = await CreateAuthenticatedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/todo", new { title = "Delete Test" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Soft delete
        var softDeleteResponse = await client.DeleteAsync($"/api/v1/todo/{created!.Id}/soft");
        softDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should not appear in normal list
        var listResponse = await client.GetAsync("/api/v1/todo");
        var todos = await listResponse.Content.ReadFromJsonAsync<List<TodoResponse>>();
        todos!.Should().NotContain(t => t.Id == created.Id);

        // Should appear with includeDeleted
        var deletedListResponse = await client.GetAsync("/api/v1/todo?includeDeleted=true");
        var deletedTodos = await deletedListResponse.Content.ReadFromJsonAsync<List<TodoResponse>>();
        deletedTodos!.Should().Contain(t => t.Id == created.Id);

        // Restore
        var restoreResponse = await client.PostAsync($"/api/v1/todo/{created.Id}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var restored = await restoreResponse.Content.ReadFromJsonAsync<TodoResponse>();
        restored!.DeletedAt.Should().BeNull();

        // Hard delete (admin - creator is OrgAdmin)
        var hardDeleteResponse = await client.DeleteAsync($"/api/v1/todo/{created.Id}");
        hardDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Should be gone completely
        var getResponse = await client.GetAsync($"/api/v1/todo/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodo_NonExistent_ShouldReturn404()
    {
        var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/todo/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodos_WithStatusFilter_ShouldFilterCorrectly()
    {
        var client = await CreateAuthenticatedClient();

        // Create and toggle one todo to Done
        var create1 = await client.PostAsJsonAsync("/api/v1/todo", new { title = "Open Todo" });
        var create2 = await client.PostAsJsonAsync("/api/v1/todo", new { title = "Done Todo" });
        var todo2 = await create2.Content.ReadFromJsonAsync<TodoResponse>();
        await client.PatchAsync($"/api/v1/todo/{todo2!.Id}/toggle", null);

        // Filter for Done only
        var response = await client.GetAsync("/api/v1/todo?status=Done");
        var todos = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        todos!.Should().OnlyContain(t => t.Status == "Done");
    }
}
