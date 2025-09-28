using Demo.Infra.Repositories;
using FastEndpoints.Security;
using FluentAssertions;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// MemoryJwtRepository 단위 테스트
/// 메모리 기반 JWT 토큰 저장소의 기능을 검증합니다
/// </summary>
public class MemoryJwtRepositoryTests
{
    private readonly MemoryJwtRepository _repository;

    public MemoryJwtRepositoryTests()
    {
        _repository = new MemoryJwtRepository();
    }

    [Fact]
    public async Task StoreTokenAsync_WithUserIdAndToken_ShouldStoreTokenSuccessfully()
    {
        // Arrange
        const string userId = "test-user-id";
        const string refreshToken = "test-refresh-token";

        // Act
        await _repository.StoreTokenAsync(userId, refreshToken);

        // Assert
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StoreTokenAsync_WithTokenResponse_ShouldStoreTokenSuccessfully()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            UserId = "test-user-id",
            RefreshToken = "test-refresh-token",
            AccessToken = "test-access-token"
        };

        // Act
        await _repository.StoreTokenAsync(tokenResponse);

        // Assert
        var isValid = await _repository.TokenIsValidAsync(tokenResponse.UserId, tokenResponse.RefreshToken);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task StoreTokenAsync_WithSameUserIdTwice_ShouldUpdateToken()
    {
        // Arrange
        const string userId = "test-user-id";
        const string firstToken = "first-refresh-token";
        const string secondToken = "second-refresh-token";

        // Act
        await _repository.StoreTokenAsync(userId, firstToken);
        await _repository.StoreTokenAsync(userId, secondToken);

        // Assert
        var firstTokenValid = await _repository.TokenIsValidAsync(userId, firstToken);
        var secondTokenValid = await _repository.TokenIsValidAsync(userId, secondToken);

        firstTokenValid.Should().BeFalse();
        secondTokenValid.Should().BeTrue();
    }

    [Fact]
    public async Task TokenIsValidAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        const string userId = "test-user-id";
        const string refreshToken = "test-refresh-token";
        await _repository.StoreTokenAsync(userId, refreshToken);

        // Act
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task TokenIsValidAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        const string userId = "test-user-id";
        const string storedToken = "stored-refresh-token";
        const string invalidToken = "invalid-refresh-token";
        await _repository.StoreTokenAsync(userId, storedToken);

        // Act
        var isValid = await _repository.TokenIsValidAsync(userId, invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task TokenIsValidAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        const string userId = "non-existent-user";
        const string refreshToken = "some-token";

        // Act
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("user1", "token1")]
    [InlineData("user2", "token2")]
    [InlineData("admin", "admin-token")]
    [InlineData("guest", "guest-token")]
    public async Task StoreAndValidateToken_WithDifferentUsers_ShouldWorkIndependently(
        string userId, string refreshToken)
    {
        // Act
        await _repository.StoreTokenAsync(userId, refreshToken);

        // Assert
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleUsers_ShouldNotInterfereWithEachOther()
    {
        // Arrange
        const string user1 = "user1";
        const string user2 = "user2";
        const string token1 = "token1";
        const string token2 = "token2";

        // Act
        await _repository.StoreTokenAsync(user1, token1);
        await _repository.StoreTokenAsync(user2, token2);

        // Assert
        var user1Valid = await _repository.TokenIsValidAsync(user1, token1);
        var user2Valid = await _repository.TokenIsValidAsync(user2, token2);
        var user1WrongToken = await _repository.TokenIsValidAsync(user1, token2);
        var user2WrongToken = await _repository.TokenIsValidAsync(user2, token1);

        user1Valid.Should().BeTrue();
        user2Valid.Should().BeTrue();
        user1WrongToken.Should().BeFalse();
        user2WrongToken.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("very-long-token-string-with-special-characters-!@#$%^&*()")]
    public async Task StoreTokenAsync_WithDifferentTokenFormats_ShouldHandleCorrectly(string refreshToken)
    {
        // Arrange
        const string userId = "test-user";

        // Act
        await _repository.StoreTokenAsync(userId, refreshToken);

        // Assert
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task TokenResponse_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            UserId = "test-user",
            RefreshToken = null!, // Simulating potential null value
            AccessToken = "test-access-token"
        };

        // Act & Assert - Should not throw exception
        await _repository.StoreTokenAsync(tokenResponse);
        var isValid = await _repository.TokenIsValidAsync(tokenResponse.UserId, null!);
        isValid.Should().BeTrue(); // null matches null
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        const string userId = "concurrent-user";
        const int numberOfTasks = 10;
        var tasks = new List<Task>();

        // Act - Multiple concurrent store operations
        for (int i = 0; i < numberOfTasks; i++)
        {
            var token = $"token-{i}";
            tasks.Add(_repository.StoreTokenAsync(userId, token));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have one of the tokens stored (the last one due to concurrent updates)
        var hasValidToken = false;
        for (int i = 0; i < numberOfTasks; i++)
        {
            var token = $"token-{i}";
            if (await _repository.TokenIsValidAsync(userId, token))
            {
                hasValidToken = true;
                break;
            }
        }

        hasValidToken.Should().BeTrue("At least one token should be valid after concurrent operations");
    }
}