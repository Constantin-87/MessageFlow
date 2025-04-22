using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MessageFlow.Server.Controllers;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.UserManagement.Queries;
using MessageFlow.Server.MediatR.UserManagement.Commands;

namespace MessageFlow.Tests.Tests.Server.Controllers;

public class UserManagementControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly UserManagementController _controller;

    public UserManagementControllerTests()
    {
        _controller = new UserManagementController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var users = new List<ApplicationUserDTO> { new() { Id = "u1" } };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), default)).ReturnsAsync(users);

        var result = await _controller.GetUsers();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, ok.Value);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser()
    {
        var user = new ApplicationUserDTO { Id = "u1" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default)).ReturnsAsync(user);

        var result = await _controller.GetUserById("u1");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(user, ok.Value);
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), default)).ReturnsAsync((ApplicationUserDTO?)null);

        var result = await _controller.GetUserById("u1");
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_ReturnsOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), default)).ReturnsAsync((true, null));

        var result = await _controller.CreateUser(new ApplicationUserDTO());

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("User created successfully", json);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), default)).ReturnsAsync((false, "fail"));

        var result = await _controller.CreateUser(new ApplicationUserDTO());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("fail", bad.Value);
    }

    [Fact]
    public async Task UpdateUser_ReturnsOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), default)).ReturnsAsync((true, null));

        var result = await _controller.UpdateUser("u1", new ApplicationUserDTO());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("{ message = User updated successfully }", ok.Value!.ToString());
    }

    [Fact]
    public async Task UpdateUser_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), default)).ReturnsAsync((false, "fail"));

        var result = await _controller.UpdateUser("u1", new ApplicationUserDTO());
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("fail", bad.Value);
    }

    [Fact]
    public async Task DeleteUser_ReturnsOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), default)).ReturnsAsync(true);

        var result = await _controller.DeleteUser("u1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("User deleted successfully", json);
    }

    [Fact]
    public async Task DeleteUser_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), default)).ReturnsAsync(false);

        var result = await _controller.DeleteUser("u1");
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to delete user.", bad.Value);
    }

    [Fact]
    public async Task DeleteUsersByCompanyId_ReturnsOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), default)).ReturnsAsync(true);

        var result = await _controller.DeleteUsersByCompanyId("c1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new { message = "All users for this company were deleted successfully." }.ToString(), ok.Value!.ToString());
    }

    [Fact]
    public async Task DeleteUsersByCompanyId_ReturnsBadRequest()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUsersByCompanyCommand>(), default)).ReturnsAsync(false);

        var result = await _controller.DeleteUsersByCompanyId("c1");
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to delete users for the specified company.", bad.Value);
    }

    [Fact]
    public async Task GetAvailableRoles_ReturnsOk()
    {
        var roles = new List<string> { "Admin", "User" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAvailableRolesQuery>(), default)).ReturnsAsync(roles);

        var result = await _controller.GetAvailableRoles();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(roles, ok.Value);
    }

    [Fact]
    public async Task GetUsersForCompany_ReturnsOk()
    {
        var users = new List<ApplicationUserDTO> { new() { Id = "u1" } };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersForCompanyQuery>(), default)).ReturnsAsync(users);

        var result = await _controller.GetUsersForCompany("c1");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, ok.Value);
    }

    [Fact]
    public async Task GetUsersForCompany_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersForCompanyQuery>(), default)).ReturnsAsync(new List<ApplicationUserDTO>());

        var result = await _controller.GetUsersForCompany("c1");
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUsersByIds_ReturnsOk()
    {
        var users = new List<ApplicationUserDTO> { new() { Id = "u1" } };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersByIdsQuery>(), default)).ReturnsAsync(users);

        var result = await _controller.GetUsersByIds(new List<string> { "u1" });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(users, ok.Value);
    }

    [Fact]
    public async Task GetUsersByIds_ReturnsBadRequest_WhenEmpty()
    {
        var result = await _controller.GetUsersByIds(new List<string>());
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
