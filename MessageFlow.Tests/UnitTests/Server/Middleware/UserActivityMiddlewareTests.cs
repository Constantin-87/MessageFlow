using MessageFlow.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Moq.Protected;
using Moq;
using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MessageFlow.Tests.UnitTests.Server.Middleware
{
    public class UserActivityMiddlewareTests
    {
        private readonly Mock<HttpMessageHandler> _httpHandlerMock = new();
        private readonly Mock<ILogger<UserActivityMiddleware>> _loggerMock = new();
        private readonly HttpClient _httpClient;
        private readonly DefaultHttpContext _httpContext;
        private readonly RequestDelegate _nextMock;

        public UserActivityMiddlewareTests()
        {
            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://identity.test/")
            };
            _httpContext = new DefaultHttpContext();
            _nextMock = new Mock<RequestDelegate>().Object;
        }

        private UserActivityMiddleware CreateMiddleware() =>
            new UserActivityMiddleware(_nextMock, new TestHttpClientFactory(_httpClient), _loggerMock.Object);

        [Fact]
        public async Task InvokeAsync_SkipsForWebhookPath()
        {
            var middleware = CreateMiddleware();
            _httpContext.Request.Path = "/api/webhook";

            await middleware.InvokeAsync(_httpContext);

            _httpHandlerMock.Protected()
                .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvokeAsync_SendsRequest_WhenAuthenticated()
        {
            var middleware = CreateMiddleware();
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "123") }, "TestAuth"));
            _httpContext.Request.Headers["Authorization"] = "Bearer test-token";

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            await middleware.InvokeAsync(_httpContext);

            _httpHandlerMock.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("update-activity")), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvokeAsync_DoesNotSend_WhenTokenMissing()
        {
            var middleware = CreateMiddleware();
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "123") }, "TestAuth"));

            // No Authorization header
            await middleware.InvokeAsync(_httpContext);

            _httpHandlerMock.Protected()
                .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task InvokeAsync_LogsError_WhenHttpClientThrows()
        {
            var middleware = CreateMiddleware();
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "123") }, "TestAuth"));
            _httpContext.Request.Headers["Authorization"] = "Bearer test-token";

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Boom"));

            await middleware.InvokeAsync(_httpContext);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to update user activity")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_SkipsForBlazorPath()
        {
            var middleware = CreateMiddleware();
            _httpContext.Request.Path = "/_blazor";

            await middleware.InvokeAsync(_httpContext);

            _httpHandlerMock.Protected()
                .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }

    public class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

}
