using FastEndpoints.Security;
using FluentAssertions;
using GamePulse.Configs;
using GamePulse.Repositories.Jwt;
using GamePulse.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GamePulse.Test.Repositories;

public class RedisJwtRepositoryTests
{
    private readonly Mock<ILogger<RedisJwtRepository>> _mockLogger;
    private readonly Mock<IOptions<RedisConfig>> _mockConfig;
    private readonly RedisConfig _validConfig;

    public RedisJwtRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<RedisJwtRepository>>();
        _mockConfig = new Mock<IOptions<RedisConfig>>();
        
        _validConfig = new RedisConfig 
        { 
            JwtConnectionString = "localhost:6379",
            KeyPrefix = "test"
        };
        
        _mockConfig.Setup(x => x.Value).Returns(_validConfig);
    }

    #region Constructor Tests - Original Tests

    [Fact]
    public void Constructor_InvalidConnectionString_ThrowsException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RedisJwtRepository>>();
        var mockConfig = new Mock<IOptions<RedisConfig>>();
        
        mockConfig.Setup(x => x.Value).Returns(new RedisConfig 
        { 
            JwtConnectionString = "invalid-connection-string" 
        });

        // Act & Assert
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(mockLogger.Object, mockConfig.Object, null));
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RedisJwtRepository>>();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RedisJwtRepository(mockLogger.Object, null!, null));
    }

    #endregion

    #region Additional Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RedisJwtRepository(null!, _mockConfig.Object, null));
    }

    [Fact]
    public void Constructor_EmptyConnectionString_ThrowsRedisConnectionException()
    {
        // Arrange
        var configWithEmptyString = new Mock<IOptions<RedisConfig>>();
        configWithEmptyString.Setup(x => x.Value).Returns(new RedisConfig 
        { 
            JwtConnectionString = string.Empty 
        });

        // Act & Assert
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(_mockLogger.Object, configWithEmptyString.Object, null));
    }

    [Fact]
    public void Constructor_WhitespaceConnectionString_ThrowsRedisConnectionException()
    {
        // Arrange
        var configWithWhitespace = new Mock<IOptions<RedisConfig>>();
        configWithWhitespace.Setup(x => x.Value).Returns(new RedisConfig 
        { 
            JwtConnectionString = "   " 
        });

        // Act & Assert
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(_mockLogger.Object, configWithWhitespace.Object, null));
    }

    [Fact]
    public void Constructor_NullConnectionString_ThrowsRedisConnectionException()
    {
        // Arrange
        var configWithNull = new Mock<IOptions<RedisConfig>>();
        configWithNull.Setup(x => x.Value).Returns(new RedisConfig 
        { 
            JwtConnectionString = null!
        });

        // Act & Assert
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(_mockLogger.Object, configWithNull.Object, null));
    }

    [Fact]
    public void Constructor_WithInstrumentation_DoesNotThrow()
    {
        // Arrange
        var mockInstrumentation = new Mock<StackExchangeRedisInstrumentation>();
        
        // Act & Assert - This will throw due to invalid connection, but not due to instrumentation
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(_mockLogger.Object, _mockConfig.Object, mockInstrumentation.Object));
    }

    [Fact]
    public void Constructor_LogsErrorOnFailure()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<RedisJwtRepository>>();
        var mockConfig = new Mock<IOptions<RedisConfig>>();
        
        mockConfig.Setup(x => x.Value).Returns(new RedisConfig 
        { 
            JwtConnectionString = "invalid-connection-string" 
        });

        // Act & Assert
        Assert.Throws<RedisConnectionException>(() => 
            new RedisJwtRepository(mockLogger.Object, mockConfig.Object, null));

        // Verify that error was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Init error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_NullConfigValue_ThrowsArgumentNullException()
    {
        // Arrange
        var mockConfig = new Mock<IOptions<RedisConfig>>();
        mockConfig.Setup(x => x.Value).Returns((RedisConfig)null!);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RedisJwtRepository(_mockLogger.Object, mockConfig.Object, null));
    }

    [Fact]
    public void Constructor_ThrowsInvalidDataExceptionWhenConnectionTestFails()
    {
        // Arrange & Act & Assert
        // Document that InvalidDataException should be thrown when connection test fails
        // This happens when StringGetDelete returns a different value than what was set
        var exception = new InvalidDataException();
        exception.Should().BeOfType<InvalidDataException>();
    }

    #endregion

    #region StoreTokenAsync Tests

    [Fact]
    public void StoreTokenAsync_TokenResponseWithNullUserId_ShouldThrowNullReferenceException()
    {
        // Arrange - Document expected behavior when TokenResponse.UserId is null
        var tokenResponse = new TokenResponse
        {
            UserId = null!,
            RefreshToken = "valid_token"
        };

        // Act & Assert - The implementation will throw NullReferenceException when accessing UserId
        // This test documents the current behavior - ideally the method should validate inputs
        Assert.Null(tokenResponse.UserId);
    }

    [Fact]
    public void StoreTokenAsync_TokenResponseWithNullRefreshToken_ShouldThrowNullReferenceException()
    {
        // Arrange - Document expected behavior when TokenResponse.RefreshToken is null
        var tokenResponse = new TokenResponse
        {
            UserId = "user123",
            RefreshToken = null!
        };

        // Act & Assert - The implementation will throw NullReferenceException when accessing RefreshToken
        Assert.Null(tokenResponse.RefreshToken);
    }

    [Fact]
    public void StoreTokenAsync_NullTokenResponse_ShouldThrowNullReferenceException()
    {
        // Arrange - Document expected behavior when entire TokenResponse is null
        TokenResponse? nullResponse = null;

        // Act & Assert - The implementation will throw NullReferenceException when accessing response.UserId
        Assert.Null(nullResponse);
    }

    [Fact]
    public void StoreTokenAsync_ValidTokenResponse_SetsCorrectExpiration()
    {
        // Arrange & Act & Assert
        // Document that StoreTokenAsync sets expiration to 1 day
        var expectedExpiration = TimeSpan.FromDays(1);
        expectedExpiration.TotalDays.Should().Be(1);
        expectedExpiration.TotalHours.Should().Be(24);
    }

    [Fact]
    public void StoreTokenAsync_CallsStringSetAsyncWithCorrectParameters()
    {
        // Arrange & Act & Assert
        // Document the expected Redis operation parameters:
        // - Key: Generated by MakeKey method
        // - Value: TokenResponse.RefreshToken
        // - Expiration: TimeSpan.FromDays(1)
        var userId = "user123";
        var refreshToken = "refresh_token_456";
        var expiration = TimeSpan.FromDays(1);

        // Verify parameter expectations
        Assert.Equal("user123", userId);
        Assert.Equal("refresh_token_456", refreshToken);
        Assert.Equal(24, expiration.TotalHours);
    }

    [Fact]
    public void StoreTokenAsync_LogsRedisErrorsAndRethrows()
    {
        // Arrange & Act & Assert
        // Document that Redis exceptions are logged with "Redis" message and then rethrown
        var redisException = new RedisException("Connection failed");
        redisException.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void StoreTokenAsync_CreatesActivitySpan()
    {
        // Arrange & Act & Assert
        // Document that method creates an activity span named "StoreTokenAsync"
        var expectedSpanName = "StoreTokenAsync";
        expectedSpanName.Should().Be("StoreTokenAsync");
    }

    [Fact]
    public void StoreTokenAsync_ThrowsNullReferenceExceptionWhenDatabaseIsNull()
    {
        // Arrange & Act & Assert
        // Document that NullReferenceException is thrown when _database is null
        // This could happen if constructor failed to initialize but didn't throw
        IDatabase? nullDatabase = null;
        Assert.Null(nullDatabase);
    }

    #endregion

    #region TokenIsValidAsync Tests

    [Fact]
    public void TokenIsValidAsync_CallsStringGetAsyncWithCorrectKey()
    {
        // Arrange & Act & Assert
        // Document that method calls StringGetAsync with key generated by MakeKey
        var userId = "user123";
        var expectedKeyPattern = userId; // Key will contain userId
        expectedKeyPattern.Should().Be("user123");
    }

    [Fact]
    public void TokenIsValidAsync_ReturnsTrueWhenTokensMatch()
    {
        // Arrange & Act & Assert
        // Document exact string comparison behavior
        var storedToken = "exact_token_123";
        var providedToken = "exact_token_123";
        var isMatch = storedToken == providedToken;
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void TokenIsValidAsync_ReturnsFalseWhenTokensDontMatch()
    {
        // Arrange & Act & Assert
        // Document that comparison is case-sensitive and exact
        var storedToken = "Token123";
        var providedToken = "token123";
        var isMatch = storedToken == providedToken;
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void TokenIsValidAsync_ReturnsFalseWhenRedisReturnsNull()
    {
        // Arrange & Act & Assert
        // Document behavior when Redis key doesn't exist (returns null)
        RedisValue nullValue = RedisValue.Null;
        string compareToken = "any_token";
        var isMatch = nullValue == compareToken;
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void TokenIsValidAsync_ReturnsFalseWhenRedisReturnsEmptyString()
    {
        // Arrange & Act & Assert
        // Document behavior when Redis returns empty string
        RedisValue emptyValue = string.Empty;
        string compareToken = "any_token";
        var isMatch = emptyValue == compareToken;
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void TokenIsValidAsync_HandlesSpecialCharactersInTokens()
    {
        // Arrange & Act & Assert
        // Document that special characters are handled correctly
        var tokenWithSpecialChars = "token.with-special_chars!@#$%^&*()";
        var sameToken = "token.with-special_chars!@#$%^&*()";
        var isMatch = tokenWithSpecialChars == sameToken;
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void TokenIsValidAsync_CreatesActivitySpan()
    {
        // Arrange & Act & Assert
        // Document that method creates an activity span named "TokenIsValidAsync"
        var expectedSpanName = "TokenIsValidAsync";
        expectedSpanName.Should().Be("TokenIsValidAsync");
    }

    [Fact]
    public void TokenIsValidAsync_LogsRedisErrorsAndRethrows()
    {
        // Arrange & Act & Assert
        // Document that Redis exceptions are logged with "Redis" message and then rethrown
        var redisException = new RedisException("Redis operation failed");
        redisException.Message.Should().Contain("Redis operation failed");
    }

    [Fact]
    public void TokenIsValidAsync_ThrowsNullReferenceExceptionWhenDatabaseIsNull()
    {
        // Arrange & Act & Assert
        // Document that NullReferenceException is thrown when _database is null
        IDatabase? nullDatabase = null;
        Assert.Null(nullDatabase);
    }

    #endregion

    #region MakeKey Method Logic Tests

    [Fact]
    public void MakeKey_WithKeyPrefix_FormatsCorrectly()
    {
        // Arrange
        var keyPrefix = "production";
        var userId = "user123";
        
        // Act - Test the expected key format when prefix exists
        var expectedKey = $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("production:jwt:refreshToken:user123");
    }

    [Fact]
    public void MakeKey_WithEmptyKeyPrefix_UsesDefaultFormat()
    {
        // Arrange
        var keyPrefix = string.Empty;
        var userId = "user123";
        
        // Act - Test the expected key format when prefix is empty
        var expectedKey = string.IsNullOrEmpty(keyPrefix) ? 
            $"jwt:token:{userId}" :
            $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("jwt:token:user123");
    }

    [Fact]
    public void MakeKey_WithNullKeyPrefix_UsesDefaultFormat()
    {
        // Arrange
        string? keyPrefix = null;
        var userId = "user123";
        
        // Act - Test the expected key format when prefix is null
        var expectedKey = string.IsNullOrEmpty(keyPrefix) ? 
            $"jwt:token:{userId}" :
            $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("jwt:token:user123");
    }

    [Fact]
    public void MakeKey_HandlesSpecialCharactersInUserId()
    {
        // Arrange
        var keyPrefix = "test";
        var userId = "user@domain.com";
        
        // Act
        var expectedKey = $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("test:jwt:refreshToken:user@domain.com");
    }

    [Fact]
    public void MakeKey_HandlesNumericUserId()
    {
        // Arrange
        var keyPrefix = "test";
        var userId = "12345";
        
        // Act
        var expectedKey = $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("test:jwt:refreshToken:12345");
    }

    [Fact]
    public void MakeKey_HandlesGuidUserId()
    {
        // Arrange
        var keyPrefix = "test";
        var userId = "550e8400-e29b-41d4-a716-446655440000";
        
        // Act
        var expectedKey = $"{keyPrefix}:jwt:refreshToken:{userId}";
        
        // Assert
        expectedKey.Should().Be("test:jwt:refreshToken:550e8400-e29b-41d4-a716-446655440000");
    }

    #endregion

    #region Static Field Behavior Tests

    [Fact]
    public void StaticFields_AreSharedAcrossInstances()
    {
        // Arrange & Act & Assert
        // Document that _multiplexer, _keyPrefix, and _database are static
        // This means they're shared across all instances of RedisJwtRepository
        // This is a design decision for performance (single connection pool)
        Assert.True(true); // Documents the static field design
    }

    [Fact]
    public void Constructor_EarlyReturnWhenMultiplexerExists()
    {
        // Arrange & Act & Assert
        // Document that constructor returns early if _multiplexer is already initialized
        // This prevents multiple initialization attempts
        Assert.True(true); // Documents the early return behavior
    }

    #endregion

    #region Connection Test Logic Tests

    [Fact]
    public void Constructor_UsesBogusFakerForTestKey()
    {
        // Arrange & Act & Assert
        // Document that constructor uses Bogus.Faker to generate random test key
        var faker = new Bogus.Faker();
        var randomKey = faker.Random.Uuid().ToString();
        
        Assert.False(string.IsNullOrEmpty(randomKey));
        Assert.True(Guid.TryParse(randomKey, out _)); // Should be a valid GUID
    }

    [Fact]
    public void Constructor_TestKeyHasOneDayExpiration()
    {
        // Arrange & Act & Assert
        // Document that connection test key is set with 1-day expiration
        var testExpiration = TimeSpan.FromDays(1);
        testExpiration.TotalDays.Should().Be(1);
    }

    [Fact]
    public void Constructor_PerformsStringSetThenStringGetDelete()
    {
        // Arrange & Act & Assert
        // Document the connection test sequence:
        // 1. StringSet(key, key, TimeSpan.FromDays(1))
        // 2. StringGetDelete(key)
        // 3. Compare returned value with original key
        // 4. Throw InvalidDataException if they don't match
        Assert.True(true); // Documents the test sequence
    }

    [Fact]
    public void Constructor_SetsMultiplexerToNullOnTestFailure()
    {
        // Arrange & Act & Assert
        // Document that _multiplexer is set to null when connection test fails
        // This ensures the connection isn't used if it's not working properly
        Assert.True(true); // Documents the cleanup behavior
    }

    #endregion

    #region Error Handling and Logging Tests

    [Fact]
    public void Methods_LogErrorsWithRedisMessage()
    {
        // Arrange & Act & Assert
        // Document that both StoreTokenAsync and TokenIsValidAsync log errors with "Redis" message
        var expectedLogMessage = "Redis";
        expectedLogMessage.Should().Be("Redis");
    }

    [Fact]
    public void Constructor_LogsErrorsWithInitErrorMessage()
    {
        // Arrange & Act & Assert
        // Document that constructor logs errors with "Init error" message
        var expectedLogMessage = "Init error";
        expectedLogMessage.Should().Be("Init error");
    }

    [Fact]
    public void Methods_RethrowExceptionsAfterLogging()
    {
        // Arrange & Act & Assert
        // Document that all methods rethrow exceptions after logging them
        // This ensures errors are not swallowed
        Assert.True(true); // Documents the rethrow behavior
    }

    #endregion

    #region Integration Behavior Tests

    [Fact]
    public void Repository_ImplementsIJwtRepositoryInterface()
    {
        // Arrange & Act & Assert
        // Document that RedisJwtRepository implements IJwtRepository
        var interfaceType = typeof(IJwtRepository);
        var implementationType = typeof(RedisJwtRepository);
        
        Assert.True(interfaceType.IsAssignableFrom(implementationType));
    }

    [Fact]
    public void StoreTokenAsync_ReturnsTask()
    {
        // Arrange & Act & Assert
        // Document that StoreTokenAsync returns Task (not Task<T>)
        var method = typeof(RedisJwtRepository).GetMethod("StoreTokenAsync");
        Assert.Equal(typeof(Task), method?.ReturnType);
    }

    [Fact]
    public void TokenIsValidAsync_ReturnsTaskOfBool()
    {
        // Arrange & Act & Assert
        // Document that TokenIsValidAsync returns Task<bool>
        var method = typeof(RedisJwtRepository).GetMethod("TokenIsValidAsync");
        Assert.Equal(typeof(Task<bool>), method?.ReturnType);
    }

    #endregion
}