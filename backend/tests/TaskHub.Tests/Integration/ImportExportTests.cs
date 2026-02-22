using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TaskHub.Api.Dto;
using TaskHub.Core.ImportExportEntities;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Integration;

public class ImportExportTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public ImportExportTests(TaskHubWebApplicationFactory factory)
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
    public async Task ExportJson_ShouldReturnFile()
    {
        var client = await CreateAuthenticatedClient();

        // Create a todo first
        await client.PostAsJsonAsync("/api/v1/todo", new { title = "Export Me" });

        var response = await client.GetAsync("/api/v1/importexport/export?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ExportCsv_ShouldReturnFile()
    {
        var client = await CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/api/v1/todo", new { title = "Export CSV" });

        var response = await client.GetAsync("/api/v1/importexport/export?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ImportJson_ShouldImportTodos()
    {
        var client = await CreateAuthenticatedClient();

        var todosToImport = new List<object>
        {
            new { clientProvidedId = "import-1", title = "Imported Todo 1", status = "Open", priority = "High" },
            new { clientProvidedId = "import-2", title = "Imported Todo 2", status = "Open", priority = "Low" }
        };

        var response = await client.PostAsJsonAsync("/api/v1/importexport/import/json", todosToImport);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result.Should().NotBeNull();
        result!.AcceptedCount.Should().Be(2);
        result.RejectedCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportJsonFile_ShouldImportTodos()
    {
        var client = await CreateAuthenticatedClient();

        var todosToImport = new List<object>
        {
            new { clientProvidedId = "file-1", title = "File Import 1", status = "Open", priority = "Medium" }
        };

        var json = JsonSerializer.Serialize(todosToImport);
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", "todos.json");

        var response = await client.PostAsync("/api/v1/importexport/import?format=json", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>();
        result!.AcceptedCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetTemplate_Json_ShouldReturnTemplate()
    {
        var client = _factory.CreateClientWithCookies();

        var response = await client.GetAsync("/api/v1/importexport/template?format=json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
