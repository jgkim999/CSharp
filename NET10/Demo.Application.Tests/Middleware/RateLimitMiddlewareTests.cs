using System.Text;
using System.Text.Json;
using Demo.Application.Configs;
using Demo.Application.DTO;
using Demo.Application.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Demo.Application.Tests.Middleware;

/// <summary>
/// RateLimitMiddleware 클래스의 단위 테스트
/// Rate Limiting 미들웨어의 로깅 및 응답 처리 테스트
/// </summary>
public class RateLimitMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitMiddleware>> _mockLogger;
    private readonly Mock<IOptions<RateLimitConfig>> _mockRateLimitConfig;
    private readonly RateLimitConfig _rateLimitConfig;
    private readonly Mock<RequestDelegate> _mockNext;

    public RateLimitMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RateLimitMiddleware>>();
        _mockRateLimitConfig = new Mock<IOptions<RateLimitConfig>>();
        _mockNext = new Mock<RequestDelegate>();

        // Configure RateLimitConfig
        _rateLimitConfig = new RateLimitConfig
        {
            Global = new GlobalRateLimitConfig
            {
                EnableLogging = true,
                LogRateLimitApplied = true,
                LogRateLimitExceeded = true,
                IncludeRequestCountInLogs = true,
                IncludeClientIpInLogs = true
            },
            UserCreateEndpoint = new UserCreateEndpointConfig
            {
                HitLimit = 5,
                DurationSeconds = 60,
                RetryAfterSeconds = 60,
                ErrorMessage = "요청 한도를 초과했습니다."
            }
        };

        _mockRateLimitConfig.Setup(x => x.Value).Returns(_rateLimitConfig);
    }

    [Fact]
    public async Task InvokeAsync_WithNonRateLimitedEndpoint_ShouldCallNext()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/other");

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(200); // Default status
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitedEndpoint_ShouldLogRequestInfo()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);

        // Verify that logging was called (request info logging)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit applied for IP")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitResponse_ShouldHandleCustomResponse()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        // Mock next middleware to set 429 status code
        _mockNext.Setup(x => x(context)).Callback(() => context.Response.StatusCode = 429);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers.Should().ContainKey("Retry-After");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Window");
        context.Response.Headers["Retry-After"].Should().Contain("60");
        context.Response.Headers["X-RateLimit-Limit"].Should().Contain("5");
        context.Response.Headers["X-RateLimit-Window"].Should().Contain("60");
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitResponse_ShouldLogWarning()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(context)).Callback(() => context.Response.StatusCode = 429);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded for IP")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLoggingDisabled_ShouldNotLog()
    {
        // Arrange
        _rateLimitConfig.Global.EnableLogging = false;
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitExceededLoggingDisabled_ShouldNotLogWarning()
    {
        // Arrange
        _rateLimitConfig.Global.LogRateLimitExceeded = false;
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(context)).Callback(() => context.Response.StatusCode = 429);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Theory]
    [InlineData("/api/user/create")]
    [InlineData("/API/USER/CREATE")]
    [InlineData("/Api/User/Create")]
    public async Task InvokeAsync_WithDifferentCasing_ShouldRecognizeRateLimitedEndpoint(string path)
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext(path);

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Should log because it's recognized as a rate-limited endpoint
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit applied for IP")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithXForwardedForHeader_ShouldUseForwardedIp()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1";

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Should log with the forwarded IP
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("192.168.1.100")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutRequestCountInLogs_ShouldNotIncludeRequestCount()
    {
        // Arrange
        _rateLimitConfig.Global.IncludeRequestCountInLogs = false;
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("RequestCount")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutClientIpInLogs_ShouldUseConnectionIp()
    {
        // Arrange
        _rateLimitConfig.Global.IncludeClientIpInLogs = false;
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100";

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // Should not use X-Forwarded-For when IncludeClientIpInLogs is false
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("127.0.0.1")), // Connection IP
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldPropagateException()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext("/api/user/create");
        var expectedException = new InvalidOperationException("Test exception");

        _mockNext.Setup(x => x(context)).ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        exception.Should().Be(expectedException);
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/products")]
    [InlineData("/health")]
    [InlineData("/swagger")]
    public async Task InvokeAsync_WithNonRateLimitedPaths_ShouldNotLog(string path)
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context = CreateHttpContext(path);

        _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleRequests_ShouldTrackRequestCounts()
    {
        // Arrange
        var middleware = new RateLimitMiddleware(_mockNext.Object, _mockLogger.Object, _mockRateLimitConfig.Object);
        var context1 = CreateHttpContext("/api/user/create");
        var context2 = CreateHttpContext("/api/user/create");

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        // Assert
        // Should log twice for the same endpoint
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit applied for IP")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Helper method to create HttpContext for testing
    /// </summary>
    private static HttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        // Set up response body stream
        context.Response.Body = new MemoryStream();

        return context;
    }
}