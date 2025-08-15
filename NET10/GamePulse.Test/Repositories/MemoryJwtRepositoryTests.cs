using FastEndpoints.Security;
using FluentAssertions;
using Demo.Infra.Repositories;

namespace GamePulse.Test.Repositories;

public class MemoryJwtRepositoryTests
{
    private readonly MemoryJwtRepository _repository;

    public MemoryJwtRepositoryTests()
    {
        _repository = new MemoryJwtRepository();
    }

    [Fact]
    public async Task StoreTokenAsync_ShouldStoreToken()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            UserId = "user123",
            RefreshToken = "refresh_token_123"
        };

        // Act
        await _repository.StoreTokenAsync(tokenResponse);
        var isValid = await _repository.TokenIsValidAsync("user123", "refresh_token_123");

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task TokenIsValidAsync_ValidToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            UserId = "user123",
            RefreshToken = "refresh_token_123"
        };
        await _repository.StoreTokenAsync(tokenResponse);

        // Act
        var result = await _repository.TokenIsValidAsync("user123", "refresh_token_123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TokenIsValidAsync_InvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            UserId = "user123",
            RefreshToken = "refresh_token_123"
        };
        await _repository.StoreTokenAsync(tokenResponse);

        // Act
        var result = await _repository.TokenIsValidAsync("user123", "wrong_token");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TokenIsValidAsync_NonExistentUser_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.TokenIsValidAsync("nonexistent", "any_token");

        // Assert
        result.Should().BeFalse();
    }
}