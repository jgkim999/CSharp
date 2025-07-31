using FluentAssertions;
using GamePulse.Services;
using Moq;
using OpenTelemetry.Trace;
using System.Diagnostics;
using OpenTelemetry;

namespace GamePulse.Test.Services;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var tracerProvider = Sdk.CreateTracerProviderBuilder().Build();
        var tracer = tracerProvider.GetTracer("test");
        _authService = new AuthService(tracer);
    }

    [Fact]
    public async Task CredentialsAreValidAsync_ValidPassword_ReturnsTrue()
    {
        // Arrange
        var username = "testuser";
        var password = "admin";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsAreValidAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";

        // Act
        var result = await _authService.CredentialsAreValidAsync(username, password, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}