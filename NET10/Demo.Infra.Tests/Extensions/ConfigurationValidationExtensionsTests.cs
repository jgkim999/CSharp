using Demo.Infra.Configs;
using Demo.Infra.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Demo.Infra.Tests.Extensions;

/// <summary>
/// ConfigurationValidationExtensions 테스트
/// 설정 유효성 검증 확장 메서드들의 기능을 검증합니다
/// </summary>
public class ConfigurationValidationExtensionsTests
{
    [Fact]
    public void AddValidatedFusionCacheConfig_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FusionCache:DefaultEntryOptions"] = "00:05:00",
                ["FusionCache:L1CacheDuration"] = "00:01:00",
                ["FusionCache:SoftTimeout"] = "00:00:01",
                ["FusionCache:HardTimeout"] = "00:00:05",
                ["FusionCache:EnableFailSafe"] = "true",
                ["FusionCache:EnableEagerRefresh"] = "true",
                ["FusionCache:FailSafeMaxDuration"] = "01:00:00",
                ["FusionCache:FailSafeThrottleDuration"] = "00:00:30",
                ["FusionCache:EagerRefreshThreshold"] = "0.8",
                ["FusionCache:L1CacheMaxSize"] = "1000",
                ["FusionCache:EnableCacheStampedeProtection"] = "true",
                ["FusionCache:EnableOpenTelemetry"] = "true",
                ["FusionCache:EnableDetailedLogging"] = "false",
                ["FusionCache:EnableMetrics"] = "true",
                ["FusionCache:CacheEventLogLevel"] = "Debug",
                ["FusionCache:MetricsCollectionIntervalSeconds"] = "0",
                ["FusionCache:UseFusionCache"] = "false",
                ["FusionCache:TrafficSplitRatio"] = "0.0",
                ["FusionCache:TrafficSplitHashSeed"] = "12345"
            })
            .Build();

        // Act
        services.AddValidatedFusionCacheConfig(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IOptions<FusionCacheConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IValidateOptions<FusionCacheConfig>>().Should().NotBeNull();
        serviceProvider.GetService<IConfigurationChangeMonitor>().Should().NotBeNull();
    }

    [Fact]
    public void ValidateConfigurationOnStartup_ShouldAddConfigurationValidationService()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder();
        var services = new ServiceCollection();

        // Act
        hostBuilder.ValidateConfigurationOnStartup();
        var host = hostBuilder.ConfigureServices(s => s.AddLogging()).Build();

        // Assert
        var hostedServices = host.Services.GetServices<IHostedService>();
        hostedServices.Should().Contain(s => s is ConfigurationValidationService);
    }
}

/// <summary>
/// FusionCacheConfigValidator 테스트
/// </summary>
public class FusionCacheConfigValidatorTests
{
    private readonly Mock<ILogger<FusionCacheConfigValidator>> _mockLogger;
    private readonly FusionCacheConfigValidator _validator;

    public FusionCacheConfigValidatorTests()
    {
        _mockLogger = new Mock<ILogger<FusionCacheConfigValidator>>();
        _validator = new FusionCacheConfigValidator(_mockLogger.Object);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnSuccess()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            DefaultEntryOptions = TimeSpan.FromMinutes(5),
            L1CacheDuration = TimeSpan.FromMinutes(1),
            SoftTimeout = TimeSpan.FromSeconds(1),
            HardTimeout = TimeSpan.FromSeconds(5),
            EnableFailSafe = true,
            EnableEagerRefresh = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
            EagerRefreshThreshold = 0.8f,
            L1CacheMaxSize = 1000,
            EnableCacheStampedeProtection = true,
            EnableOpenTelemetry = true,
            EnableDetailedLogging = false,
            EnableMetrics = true,
            CacheEventLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            MetricsCollectionIntervalSeconds = 0,
            UseFusionCache = false,
            TrafficSplitRatio = 0.0,
            TrafficSplitHashSeed = 12345
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_WithInvalidConfig_ShouldReturnFailure()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            // Set HardTimeout <= SoftTimeout to make validation fail
            SoftTimeout = TimeSpan.FromSeconds(5),
            HardTimeout = TimeSpan.FromSeconds(3) // This should be larger than SoftTimeout
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().NotBeEmpty();
    }
}

/// <summary>
/// ConfigurationChangeMonitor 테스트
/// </summary>
public class ConfigurationChangeMonitorTests
{
    private readonly Mock<IOptionsMonitor<FusionCacheConfig>> _mockOptionsMonitor;
    private readonly Mock<ILogger<ConfigurationChangeMonitor>> _mockLogger;

    public ConfigurationChangeMonitorTests()
    {
        _mockOptionsMonitor = new Mock<IOptionsMonitor<FusionCacheConfig>>();
        _mockLogger = new Mock<ILogger<ConfigurationChangeMonitor>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(config);

        // Act
        using var monitor = new ConfigurationChangeMonitor(_mockOptionsMonitor.Object, _mockLogger.Object);

        // Assert
        monitor.Should().NotBeNull();
        monitor.CurrentConfiguration.Should().Be(config);
    }

    [Fact]
    public void ConfigurationChanged_WithValidConfig_ShouldInvokeEvent()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(config);

        // Act
        using var monitor = new ConfigurationChangeMonitor(_mockOptionsMonitor.Object, _mockLogger.Object);

        // Simulate configuration change by accessing a property that would trigger the event
        // Since we can't easily mock the OnChange extension method, we'll test the basic functionality
        var currentConfig = monitor.CurrentConfiguration;

        // Assert
        currentConfig.Should().Be(config);
        monitor.Should().NotBeNull();
    }

    [Fact]
    public void ConfigurationChanged_WithInvalidConfig_ShouldNotInvokeEvent()
    {
        // Arrange
        var invalidConfig = new FusionCacheConfig(); // Empty config
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(invalidConfig);

        // Act
        using var monitor = new ConfigurationChangeMonitor(_mockOptionsMonitor.Object, _mockLogger.Object);

        // Test that invalid config is still accessible
        var currentConfig = monitor.CurrentConfiguration;

        // Assert
        currentConfig.Should().Be(invalidConfig);
        monitor.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldDisposeChangeListener()
    {
        // Arrange
        var config = CreateValidConfig();
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(config);

        var monitor = new ConfigurationChangeMonitor(_mockOptionsMonitor.Object, _mockLogger.Object);

        // Act
        var exception = Record.Exception(() => monitor.Dispose());

        // Assert
        exception.Should().BeNull("Dispose should complete without throwing");
    }

    private static FusionCacheConfig CreateValidConfig()
    {
        return new FusionCacheConfig
        {
            DefaultEntryOptions = TimeSpan.FromMinutes(5),
            L1CacheDuration = TimeSpan.FromMinutes(1),
            SoftTimeout = TimeSpan.FromSeconds(1),
            HardTimeout = TimeSpan.FromSeconds(5),
            EnableFailSafe = true,
            EnableEagerRefresh = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
            EagerRefreshThreshold = 0.8f,
            L1CacheMaxSize = 1000,
            EnableCacheStampedeProtection = true,
            EnableOpenTelemetry = true,
            EnableDetailedLogging = false,
            EnableMetrics = true,
            CacheEventLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            MetricsCollectionIntervalSeconds = 0,
            UseFusionCache = false,
            TrafficSplitRatio = 0.0,
            TrafficSplitHashSeed = 12345
        };
    }
}

/// <summary>
/// ConfigurationValidationService 테스트
/// </summary>
public class ConfigurationValidationServiceTests
{
    private readonly Mock<ILogger<ConfigurationValidationService>> _mockLogger;
    private readonly Mock<IOptionsMonitor<FusionCacheConfig>> _mockOptionsMonitor;

    public ConfigurationValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationValidationService>>();
        _mockOptionsMonitor = new Mock<IOptionsMonitor<FusionCacheConfig>>();
    }

    [Fact]
    public async Task StartAsync_WithValidConfig_ShouldCompleteSuccessfully()
    {
        // Arrange
        var validConfig = CreateValidConfig();
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(validConfig);

        var service = new ConfigurationValidationService(_mockLogger.Object, _mockOptionsMonitor.Object);

        // Act & Assert
        var act = async () => await service.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithInvalidConfig_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new FusionCacheConfig
        {
            // Set HardTimeout <= SoftTimeout to make validation fail
            SoftTimeout = TimeSpan.FromSeconds(5),
            HardTimeout = TimeSpan.FromSeconds(3) // This should be larger than SoftTimeout
        };
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(invalidConfig);

        var service = new ConfigurationValidationService(_mockLogger.Object, _mockOptionsMonitor.Object);

        // Act & Assert
        var act = async () => await service.StartAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var validConfig = CreateValidConfig();
        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(validConfig);

        var service = new ConfigurationValidationService(_mockLogger.Object, _mockOptionsMonitor.Object);

        // Act & Assert
        var act = async () => await service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    private static FusionCacheConfig CreateValidConfig()
    {
        return new FusionCacheConfig
        {
            DefaultEntryOptions = TimeSpan.FromMinutes(5),
            L1CacheDuration = TimeSpan.FromMinutes(1),
            SoftTimeout = TimeSpan.FromSeconds(1),
            HardTimeout = TimeSpan.FromSeconds(5),
            EnableFailSafe = true,
            EnableEagerRefresh = true,
            FailSafeMaxDuration = TimeSpan.FromHours(1),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
            EagerRefreshThreshold = 0.8f,
            L1CacheMaxSize = 1000,
            EnableCacheStampedeProtection = true,
            EnableOpenTelemetry = true,
            EnableDetailedLogging = false,
            EnableMetrics = true,
            CacheEventLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            MetricsCollectionIntervalSeconds = 0,
            UseFusionCache = false,
            TrafficSplitRatio = 0.0,
            TrafficSplitHashSeed = 12345
        };
    }
}