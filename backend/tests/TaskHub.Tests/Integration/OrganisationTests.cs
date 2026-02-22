using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskHub.Api.Dto;
using TaskHub.Tests.Helpers;
using Xunit;

namespace TaskHub.Tests.Integration;

public class OrganisationTests : IClassFixture<TaskHubWebApplicationFactory>
{
    private readonly TaskHubWebApplicationFactory _factory;

    public OrganisationTests(TaskHubWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrganisation_ShouldReturn201()
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
            name = $"Test Org {uniqueId}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var org = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        org.Should().NotBeNull();
        org!.Name.Should().Be($"Test Org {uniqueId}");
    }

    [Fact]
    public async Task GetMyOrganisations_ShouldReturnUserOrgs()
    {
        var client = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"user_{uniqueId}",
            email = $"user_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        await client.PostAsJsonAsync("/api/v1/organisations", new { name = $"Org A {uniqueId}" });
        await client.PostAsJsonAsync("/api/v1/organisations", new { name = $"Org B {uniqueId}" });

        var response = await client.GetAsync("/api/v1/organisations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orgs = await response.Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        orgs!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task AddMember_ShouldSucceedForAdmin()
    {
        // Create admin user and org
        var adminClient = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await adminClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"admin_{uniqueId}",
            email = $"admin_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        var orgResponse = await adminClient.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = $"Member Org {uniqueId}"
        });
        var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        adminClient.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());

        // Create member user
        var memberClient = _factory.CreateClientWithCookies();
        await memberClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"member_{uniqueId}",
            email = $"member_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        // Add member
        var addResponse = await adminClient.PostAsJsonAsync($"/api/v1/organisations/{org.Id}/members", new
        {
            email = $"member_{uniqueId}@test.com",
            role = "Member"
        });

        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify member appears in members list
        var membersResponse = await adminClient.GetAsync($"/api/v1/organisations/{org.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<MemberResponse>>();
        members!.Should().HaveCountGreaterThanOrEqualTo(2);
        members.Should().Contain(m => m.Email == $"member_{uniqueId}@test.com");
    }

    [Fact]
    public async Task AddMember_DuplicateEmail_ShouldReturn400()
    {
        var client = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"admin_{uniqueId}",
            email = $"admin_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        var orgResponse = await client.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = $"Dup Org {uniqueId}"
        });
        var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        client.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());

        // Try adding self (already a member as creator)
        var response = await client.PostAsJsonAsync($"/api/v1/organisations/{org.Id}/members", new
        {
            email = $"admin_{uniqueId}@test.com",
            role = "Member"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMember_NonExistentUser_ShouldReturn400()
    {
        var client = _factory.CreateClientWithCookies();
        var uniqueId = Guid.NewGuid().ToString()[..8];

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"admin_{uniqueId}",
            email = $"admin_{uniqueId}@test.com",
            password = "Test123!@#"
        });

        var orgResponse = await client.PostAsJsonAsync("/api/v1/organisations", new
        {
            name = $"NoUser Org {uniqueId}"
        });
        var org = await orgResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        client.DefaultRequestHeaders.Add("X-Organisation-Id", org!.Id.ToString());

        var response = await client.PostAsJsonAsync($"/api/v1/organisations/{org.Id}/members", new
        {
            email = "nonexistent@nowhere.com",
            role = "Member"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
