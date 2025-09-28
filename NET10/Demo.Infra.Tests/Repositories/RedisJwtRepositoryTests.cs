using Demo.Application.Configs;
using Demo.Infra.Repositories;
using FastEndpoints.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using Testcontainers.Redis;
using Xunit.Abstractions;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// RedisJwtRepository 통합 테스트
/// 실제 Valkey 컨테이너를 사용하여 Redis JWT 저장소의 기능을 검증합니다
/// Valkey는 Redis와 완전 호환되는 오픈소스 포크입니다
/// </summary>
public class RedisJwtRepositoryTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly RedisContainer _redisContainer;
    private readonly Mock<ILogger<RedisJwtRepository>> _mockLogger;
    private RedisJwtRepository? _repository;

    public RedisJwtRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<RedisJwtRepository>>();

        // Valkey 테스트 컨테이너 설정 (Redis 호환)
        _redisContainer = new RedisBuilder()
            .WithImage("valkey/valkey:8.1.3-alpine")
            .WithPortBinding(6379, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _output.WriteLine($"Valkey Container Started: {_redisContainer.GetConnectionString()}");

        // Redis 설정 생성
        var redisConfig = Options.Create(new RedisConfig
        {
            JwtConnectionString = _redisContainer.GetConnectionString(),
            KeyPrefix = "test"
        });

        try
        {
            _repository = new RedisJwtRepository(_mockLogger.Object, redisConfig, null);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to create repository: {ex.Message}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
    }

    [Fact]
    public async Task Constructor_WithValidConfig_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var redisConfig = Options.Create(new RedisConfig
        {
            JwtConnectionString = _redisContainer.GetConnectionString(),
            KeyPrefix = "test"
        });

        // Act & Assert
        var act = () => new RedisJwtRepository(_mockLogger.Object, redisConfig, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var act = () => new RedisJwtRepository(_mockLogger.Object, null, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreTokenAsync_WithValidData_ShouldStoreTokenSuccessfully()
    {
        // Arrange
        const string userId = "test-user-id";
        const string refreshToken = "test-refresh-token";

        // Act
        await _repository!.StoreTokenAsync(userId, refreshToken);

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
        await _repository!.StoreTokenAsync(tokenResponse);

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
        await _repository!.StoreTokenAsync(userId, firstToken);
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
        await _repository!.StoreTokenAsync(userId, refreshToken);

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
        await _repository!.StoreTokenAsync(userId, storedToken);

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
        var isValid = await _repository!.TokenIsValidAsync(userId, refreshToken);

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
        await _repository!.StoreTokenAsync(userId, refreshToken);

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
        await _repository!.StoreTokenAsync(user1, token1);
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
        await _repository!.StoreTokenAsync(userId, refreshToken);

        // Assert
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);
        isValid.Should().BeTrue();
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
            tasks.Add(_repository!.StoreTokenAsync(userId, token));
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

    [Fact]
    public async Task TokenExpiration_ShouldWorkCorrectly()
    {
        // Note: Redis tokens are set with 1 day expiration in the implementation
        // This test verifies the token is stored and can be retrieved immediately
        // For actual expiration testing, we would need to modify the implementation
        // to accept custom expiration times or wait for actual expiration

        // Arrange
        const string userId = "expiring-user";
        const string refreshToken = "expiring-token";

        // Act
        await _repository!.StoreTokenAsync(userId, refreshToken);

        // Assert - Token should be valid immediately after storage
        var isValid = await _repository.TokenIsValidAsync(userId, refreshToken);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task KeyPrefix_ShouldBeAppliedCorrectly()
    {
        // This test verifies that the key prefix is working by checking
        // that different repository instances with different prefixes
        // don't interfere with each other

        // Arrange
        var redisConfig2 = Options.Create(new RedisConfig
        {
            JwtConnectionString = _redisContainer.GetConnectionString(),
            KeyPrefix = "different-prefix"
        });

        var repository2 = new RedisJwtRepository(_mockLogger.Object, redisConfig2, null);

        const string userId = "prefix-test-user";
        const string token1 = "token-for-repo1";
        const string token2 = "token-for-repo2";

        // Act
        await _repository!.StoreTokenAsync(userId, token1);
        await repository2.StoreTokenAsync(userId, token2);

        // Assert
        var repo1Valid = await _repository.TokenIsValidAsync(userId, token1);
        var repo2Valid = await repository2.TokenIsValidAsync(userId, token2);
        var repo1CheckRepo2Token = await _repository.TokenIsValidAsync(userId, token2);
        var repo2CheckRepo1Token = await repository2.TokenIsValidAsync(userId, token1);

        repo1Valid.Should().BeTrue();
        repo2Valid.Should().BeTrue();
        repo1CheckRepo2Token.Should().BeFalse();
        repo2CheckRepo1Token.Should().BeFalse();
    }
}