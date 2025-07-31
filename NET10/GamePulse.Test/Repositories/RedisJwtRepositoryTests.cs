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

namespace GamePulse.Test.Repositories;

public class RedisJwtRepositoryTests
{
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
}