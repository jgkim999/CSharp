using Demo.Application.Services;
using Demo.Application.Services.Auth;
using FluentAssertions;
using Moq;
using System.Diagnostics;

namespace Demo.Application.Tests.Services.Auth;

/// <summary>
/// AuthService 클래스의 단위 테스트
/// 인증 서비스의 자격 증명 검증 기능 테스트
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<Activity> _mockActivity;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockActivity = new Mock<Activity>("test");

        // StartActivity가 호출될 때 mock activity 반환
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(_mockActivity.Object);

        _authService = new AuthService(_mockTelemetryService.Object);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithAdminPassword_ShouldReturnTrue()
    {
        // Arrange
        const string username = "testuser";
        const string password = "admin";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeTrue();
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("123456")]
    [InlineData("wrongpassword")]
    [InlineData("ADMIN")]
    [InlineData("Admin")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CredentialsAreValidAsync_WithNonAdminPassword_ShouldReturnFalse(string password)
    {
        // Arrange
        const string username = "testuser";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeFalse();
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithNullPassword_ShouldReturnFalse()
    {
        // Arrange
        const string username = "testuser";
        const string? password = null;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeFalse();
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("user123")]
    [InlineData("testuser")]
    public async Task CredentialsAreValidAsync_WithVariousUsernames_ShouldIgnoreUsername(string? username)
    {
        // Arrange
        const string password = "admin";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeTrue("username should be ignored, only password matters");
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        const string username = "testuser";
        const string password = "admin";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeTrue();
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_MultipleCalls_ShouldWorkConsistently()
    {
        // Arrange
        const string username = "testuser";
        const string validPassword = "admin";
        const string invalidPassword = "wrong";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var result1 = await _authService.CredentialsAreValidAsync(username, validPassword, cancellationToken);
        result1.Should().BeTrue();

        var result2 = await _authService.CredentialsAreValidAsync(username, invalidPassword, cancellationToken);
        result2.Should().BeFalse();

        var result3 = await _authService.CredentialsAreValidAsync(username, validPassword, cancellationToken);
        result3.Should().BeTrue();

        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Exactly(3));
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithWhitespaceAroundPassword_ShouldReturnFalse()
    {
        // Arrange
        const string username = "testuser";
        const string password = " admin ";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeFalse("password with whitespace should not be considered valid");
        _mockTelemetryService.Verify(x => x.StartActivity(nameof(AuthService), null), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullTelemetryService_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new AuthService(null));
        exception.Should().BeNull();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_WithNullTelemetryService_ShouldStillWork()
    {
        // Arrange
        var authServiceWithoutTelemetry = new AuthService(null);
        const string username = "testuser";
        const string password = "admin";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await authServiceWithoutTelemetry.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_IsAsynchronous_ShouldCompleteQuickly()
    {
        // Arrange
        const string username = "testuser";
        const string password = "admin";
        var cancellationToken = CancellationToken.None;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "method should complete quickly");
    }

    [Theory]
    [InlineData("admin\0")]
    [InlineData("admin\n")]
    [InlineData("admin\r")]
    [InlineData("admin\t")]
    public async Task CredentialsAreValidAsync_WithSpecialCharacters_ShouldReturnFalse(string password)
    {
        // Arrange
        const string username = "testuser";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, cancellationToken);

        // Assert
        result.Should().BeFalse("password with special characters should not be valid");
    }
}