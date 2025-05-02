using MessageFlow.DataAccess.Models;
using MessageFlow.Identity.MediatR.QueryHandlers;
using MessageFlow.Identity.MediatR.Queries;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using MockQueryable;

namespace MessageFlow.Tests.Tests.Identity.MediatR.Queries;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _userManagerMock = TestDbContextFactory.CreateMockUserManager(Enumerable.Empty<ApplicationUser>().AsQueryable());
        _handler = new GetCurrentUserQueryHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNull()
    {
        var users = new List<ApplicationUser>().AsQueryable().BuildMock().BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(users.Object);

        var result = await _handler.Handle(new GetCurrentUserQuery("user1"), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsDto()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "TestUser",
            CompanyId = "comp1",
            Company = new Company { Id = "comp1", CompanyName = "MyCo" },
            LastActivity = DateTime.UtcNow.AddMinutes(-5)
        };

        var users = new List<ApplicationUser> { user }.AsQueryable().BuildMock().BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(users.Object);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new[] { "Admin" });

        var result = await _handler.Handle(new GetCurrentUserQuery("user1"), default);

        Assert.NotNull(result);
        Assert.Equal("user1", result!.Id);
        Assert.Equal("TestUser", result.UserName);
        Assert.Equal("Admin", result.Role);
        Assert.Equal("comp1", result.CompanyId);
        Assert.Equal("MyCo", result.CompanyDTO?.CompanyName);
        Assert.Equal(user.LastActivity, result.LastActivity);
    }
}