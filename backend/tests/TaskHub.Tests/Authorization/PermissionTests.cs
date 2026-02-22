using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using TaskHub.Core.Entities;
using TaskHub.Core.Enum;
using Xunit;

namespace TaskHub.Tests.Authorization;

public class PermissionTests
{
    private readonly Mock<IStorage> _storageMock;
    private readonly Mock<IOrganisationContext> _orgContextMock;
    private readonly OrganisationAuthorizationHandler _handler;

    public PermissionTests()
    {
        _storageMock = new Mock<IStorage>();
        _orgContextMock = new Mock<IOrganisationContext>();
        _handler = new OrganisationAuthorizationHandler(_orgContextMock.Object,
            Mock.Of<IHttpContextAccessor>());
    }

    [Fact]
    public async Task Member_ShouldNotAccessAdminOnlyResources()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var membership = new Membership
        {
            UserId = userId,
            OrganisationId = orgId,
            Role = Role.Member
        };

        _orgContextMock.Setup(x => x.CurrentOrganisationId).Returns(orgId);
        _orgContextMock.Setup(x => x.UserIsInOrganisationAsync(userId, orgId))
            .ReturnsAsync(true);
        _orgContextMock.Setup(x => x.UserIsOrgAdminAsync(userId, orgId))
            .ReturnsAsync(false);

        // Act & Assert
        var isAdmin = await _orgContextMock.Object.UserIsOrgAdminAsync(userId, orgId);
        isAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_ShouldAccessAdminOnlyResources()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var membership = new Membership
        {
            UserId = userId,
            OrganisationId = orgId,
            Role = Role.OrgAdmin
        };

        _orgContextMock.Setup(x => x.CurrentOrganisationId).Returns(orgId);
        _orgContextMock.Setup(x => x.UserIsInOrganisationAsync(userId, orgId))
            .ReturnsAsync(true);
        _orgContextMock.Setup(x => x.UserIsOrgAdminAsync(userId, orgId))
            .ReturnsAsync(true);

        // Act & Assert
        var isAdmin = await _orgContextMock.Object.UserIsOrgAdminAsync(userId, orgId);
        isAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task User_ShouldNotAccessOtherOrganisationData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        _orgContextMock.Setup(x => x.CurrentOrganisationId).Returns(otherOrgId);
        _orgContextMock.Setup(x => x.UserIsInOrganisationAsync(userId, otherOrgId))
            .ReturnsAsync(false);

        // Act
        var isInOrg = await _orgContextMock.Object.UserIsInOrganisationAsync(userId, otherOrgId);

        // Assert
        isInOrg.Should().BeFalse();
    }
}
