using Moq;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using MessageFlow.Shared.DTOs;
using MessageFlow.Server.MediatR.Chat.GeneralProcessing.Commands;
using MessageFlow.DataAccess.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Features;
//using Xunit.Abstractions;
using Microsoft.AspNetCore.Http.Connections.Features;
using MessageFlow.DataAccess.Models;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Tests.Tests.Server.Hubs;
public class ChatHubTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<HubCallerContext> _contextMock = new();
    private readonly Mock<IHubCallerClients> _clientsMock = new();
    private readonly Mock<ILogger<ChatHub>> _loggerMock = new();
    private readonly ChatHub _hub;
    //private readonly ITestOutputHelper _output;


    public ChatHubTests(/*ITestOutputHelper output*/)
    {
        //_output = output;
        //Console.SetOut(new TestOutputTextWriter(output));
        _hub = new ChatHub(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };
    }

    

    [Fact]
    public async Task SendMessageToCustomer_CallsMediator()
    {
        // Arrange
        var dto = new MessageDTO { ConversationId = "conv1", Content = "Test" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<SendMessageToCustomerCommand>(), default))
                     .ReturnsAsync((true, "OK"));

        // Act
        await _hub.SendMessageToCustomer(dto);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<SendMessageToCustomerCommand>(c => c.MessageDto == dto), default), Times.Once);
    }

    [Fact]
    public async Task AssignConversationToUser_CallsMediator()
    {
        var userId = "user1";
        _contextMock.Setup(c => c.UserIdentifier).Returns(userId);
        _mediatorMock.Setup(m => m.Send(It.IsAny<AssignConversationToUserCommand>(), default))
                     .ReturnsAsync((true, null));

        await _hub.AssignConversationToUser("conv1");

        _mediatorMock.Verify(m => m.Send(
            It.Is<AssignConversationToUserCommand>(x => x.ConversationId == "conv1" && x.UserId == userId), default));
    }

    [Theory]
    [InlineData(true, "success")]
    [InlineData(false, "fail")]
    public async Task CloseAndAnonymizeChat_LogsResult(bool success, string message)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveConversationCommand>(), default))
                     .ReturnsAsync((success, message));

        await _hub.CloseAndAnonymizeChat("cust1");

        _mediatorMock.Verify(m => m.Send(
            It.Is<ArchiveConversationCommand>(x => x.CustomerId == "cust1"), default));
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesFromGroupsAndSendsCommand()
    {
        var connectionId = "conn1";
        var companyId = "comp1";
        var userId = "user1";

        var user = new ApplicationUserDTO
        {
            CompanyId = companyId,
            TeamsDTO = new List<TeamDTO> { new() { Id = "t1" }, new() { Id = "t2" } }
        };

        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "companyId", companyId }
        });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = query;

        var context = new TestHubCallerContext(httpContext, connectionId, userId);
        var groupsMock = new Mock<IGroupManager>();

        ChatHub.OnlineUsers[connectionId] = user;

        _hub.Context = context;
        _hub.Groups = groupsMock.Object;

        _mediatorMock.Setup(m => m.Send(It.IsAny<BroadcastUserDisconnectedCommand>(), default))
                     .ReturnsAsync(Unit.Value);

        // Act
        await _hub.OnDisconnectedAsync(null);


        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<BroadcastUserDisconnectedCommand>(x =>
                x.CompanyId == companyId && x.ConnectionId == connectionId), default), Times.Once);

        groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, $"Company_{companyId}", default), Times.Once);
        groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, "Team_t1", default), Times.Once);
        groupsMock.Verify(g => g.RemoveFromGroupAsync(connectionId, "Team_t2", default), Times.Once);
    }

    [Fact]
    public void GetQueryValue_ReturnsExpectedValue()
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
    {
        { "key1", "value1" }
    });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = query;

        var context = new TestHubCallerContext(httpContext, "conn1", "user1");
        _hub.Context = context;

        var result = _hub
            .GetType()
            .GetMethod("GetQueryValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_hub, new object[] { "key1" }) as string;

        Assert.Equal("value1", result);
    }

    [Fact]
    public async Task OnConnectedAsync_Aborts_WhenUserIsUnauthorized()
    {
        // Arrange
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal()); // no roles
        var hub = new ChatHub(_unitOfWorkMock.Object, _mapperMock.Object, _mediatorMock.Object, _loggerMock.Object)
        {
            Context = contextMock.Object,
            Clients = _clientsMock.Object
        };

        var wasAborted = false;
        contextMock.Setup(c => c.Abort()).Callback(() => wasAborted = true);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        Assert.True(wasAborted);
    }

    [Fact]
    public async Task OnConnectedAsync_Success_TriggersAllMediatorCommands()
    {
        // Arrange
        var userId = "user123";
        var companyId = "comp456";
        var connectionId = "conn789";

        var user = new ApplicationUser { Id = userId, CompanyId = companyId };
        var userDto = new ApplicationUserDTO { Id = userId, CompanyId = companyId };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Agent")
        }, "mock"));

        var callerMock = new Mock<ISingleClientProxy>();

        _contextMock.Setup(c => c.User).Returns(claimsPrincipal);
        _contextMock.Setup(c => c.UserIdentifier).Returns(userId);
        _contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        _unitOfWorkMock.Setup(u => u.ApplicationUsers.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<ApplicationUserDTO>(user)).Returns(userDto);
        _clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Unit>>(), default)).ReturnsAsync(Unit.Value);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mediatorMock.Verify(m => m.Send(It.Is<AddUserToGroupsCommand>(cmd =>
            cmd.ApplicationUser == userDto && cmd.ConnectionId == connectionId), default), Times.Once);

        _mediatorMock.Verify(m => m.Send(It.Is<LoadUserConversationsCommand>(cmd =>
            cmd.UserId == userId && cmd.CompanyId == companyId && cmd.Caller == callerMock.Object), default), Times.Once);

        _mediatorMock.Verify(m => m.Send(It.Is<BroadcastTeamMembersCommand>(cmd =>
            cmd.CompanyId == companyId), default), Times.Once);
    }

    /// <summary>
    /// Helper classes
    /// </summary>
    //private class TestOutputTextWriter : TextWriter
    //{
    //    private readonly ITestOutputHelper _output;

    //    public TestOutputTextWriter(ITestOutputHelper output)
    //    {
    //        _output = output;
    //    }

    //    public override void WriteLine(string? value)
    //    {
    //        _output.WriteLine(value ?? "");
    //    }

    //    public override void Write(char value)
    //    {
    //        _output.WriteLine(value.ToString());
    //    }

    //    public override Encoding Encoding => Encoding.UTF8;
    //}

    public class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly string _userId;
        private readonly HttpContext _httpContext;
        private readonly IFeatureCollection _features;

        public TestHubCallerContext(HttpContext httpContext, string connectionId, string userId)
        {
            _httpContext = httpContext;
            _connectionId = connectionId;
            _userId = userId;

            _features = new FeatureCollection();
            _features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            _features.Set<IHttpResponseFeature>(new HttpResponseFeature());
            _features.Set<IHttpContextFeature>(new TestHttpContextFeature(_httpContext));
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier => _userId;
        public override ClaimsPrincipal? User { get; } = new ClaimsPrincipal();
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override IFeatureCollection Features => _features;

        public override void Abort() { }
    }

    private class TestHttpContextFeature : IHttpContextFeature
    {
        public TestHttpContextFeature(HttpContext context)
        {
            HttpContext = context;
        }

        public HttpContext HttpContext { get; set; }
    }
}

public static class HubCallerContextExtensions
{
    public static HttpContext? GetHttpContext(this HubCallerContext context)
    {
        return context is ChatHubTests.TestHubCallerContext testContext
            ? testContext.GetHttpContext()
            : null;
    }
}