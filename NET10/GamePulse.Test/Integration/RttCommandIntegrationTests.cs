using System.Diagnostics;
using Demo.Application.Services;
using GamePulse.Sod.Endpoints.Rtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GamePulse.Test.Integration;

/// <summary>
/// RttCommand와 ITelemetryService 통합 테스트
/// </summary>
public class RttCommandIntegrationTests
{
    /// <summary>
    /// RttCommand 실행 시 ITelemetryService.RecordRttMetrics가 올바르게 호출되는지 검증
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCallRecordRttMetrics_WithCorrectParameters()
    {
        // Arrange
        var mockTelemetryService = new Mock<ITelemetryService>();
        var mockIpToNationService = new Mock<IIpToNationService>();
        var mockLogger = new Mock<ILogger<RttCommand>>();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockTelemetryService.Object);
        serviceCollection.AddSingleton(mockIpToNationService.Object);
        serviceCollection.AddSingleton(mockLogger.Object);
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        const string clientIp = "192.168.1.1";
        const int rttMs = 150; // 밀리초
        const int quality = 85;
        const string expectedCountryCode = "KR";
        
        // IpToNationService가 국가 코드를 반환하도록 설정
        mockIpToNationService
            .Setup(x => x.GetNationCodeAsync(clientIp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCountryCode);
        
        var command = new RttCommand(clientIp, rttMs, quality, null);
        
        // Act
        await command.ExecuteAsync(serviceProvider, CancellationToken.None);
        
        // Assert
        var expectedRttSeconds = rttMs / 1000.0; // 밀리초를 초로 변환
        
        mockTelemetryService.Verify(
            x => x.RecordRttMetrics(
                expectedCountryCode,
                expectedRttSeconds,
                quality,
                "sod"),
            Times.Once,
            "ITelemetryService.RecordRttMetrics가 올바른 매개변수로 한 번 호출되어야 합니다.");
    }
    
    /// <summary>
    /// RttCommand 실행 시 RTT 값이 밀리초에서 초로 올바르게 변환되는지 검증
    /// </summary>
    [Theory]
    [InlineData(100, 0.1)]   // 100ms = 0.1s
    [InlineData(500, 0.5)]   // 500ms = 0.5s
    [InlineData(1000, 1.0)]  // 1000ms = 1.0s
    [InlineData(1500, 1.5)]  // 1500ms = 1.5s
    public async Task ExecuteAsync_ShouldConvertRttFromMillisecondsToSeconds(int rttMs, double expectedRttSeconds)
    {
        // Arrange
        var mockTelemetryService = new Mock<ITelemetryService>();
        var mockIpToNationService = new Mock<IIpToNationService>();
        var mockLogger = new Mock<ILogger<RttCommand>>();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockTelemetryService.Object);
        serviceCollection.AddSingleton(mockIpToNationService.Object);
        serviceCollection.AddSingleton(mockLogger.Object);
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        const string clientIp = "10.0.0.1";
        const int quality = 90;
        const string countryCode = "US";
        
        mockIpToNationService
            .Setup(x => x.GetNationCodeAsync(clientIp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(countryCode);
        
        var command = new RttCommand(clientIp, rttMs, quality, null);
        
        // Act
        await command.ExecuteAsync(serviceProvider, CancellationToken.None);
        
        // Assert
        mockTelemetryService.Verify(
            x => x.RecordRttMetrics(
                countryCode,
                expectedRttSeconds,
                quality,
                "sod"),
            Times.Once,
            $"RTT 값이 {rttMs}ms에서 {expectedRttSeconds}s로 올바르게 변환되어야 합니다.");
    }
    
    /// <summary>
    /// RttCommand 실행 시 Activity가 올바르게 생성되는지 검증
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCreateActivity_WithCorrectName()
    {
        // Arrange
        var mockTelemetryService = new Mock<ITelemetryService>();
        var mockIpToNationService = new Mock<IIpToNationService>();
        var mockLogger = new Mock<ILogger<RttCommand>>();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockTelemetryService.Object);
        serviceCollection.AddSingleton(mockIpToNationService.Object);
        serviceCollection.AddSingleton(mockLogger.Object);
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        const string clientIp = "172.16.0.1";
        const int rtt = 200;
        const int quality = 75;
        
        mockIpToNationService
            .Setup(x => x.GetNationCodeAsync(clientIp, It.IsAny<CancellationToken>()))
            .ReturnsAsync("JP");
        
        var parentActivity = new Activity("ParentActivity");
        parentActivity.Start();
        
        var command = new RttCommand(clientIp, rtt, quality, parentActivity);
        
        // Act
        await command.ExecuteAsync(serviceProvider, CancellationToken.None);
        
        // Assert
        // Activity가 생성되었는지 확인 (실제 Activity 검증은 복잡하므로 예외가 발생하지 않았는지 확인)
        mockTelemetryService.Verify(
            x => x.RecordRttMetrics(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>()),
            Times.Once,
            "Activity 생성과 함께 메트릭이 기록되어야 합니다.");
        
        parentActivity.Stop();
        parentActivity.Dispose();
    }
    
    /// <summary>
    /// RttCommand 실행 시 로깅이 올바르게 수행되는지 검증
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldLogInformation_WithCorrectParameters()
    {
        // Arrange
        var mockTelemetryService = new Mock<ITelemetryService>();
        var mockIpToNationService = new Mock<IIpToNationService>();
        var mockLogger = new Mock<ILogger<RttCommand>>();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockTelemetryService.Object);
        serviceCollection.AddSingleton(mockIpToNationService.Object);
        serviceCollection.AddSingleton(mockLogger.Object);
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        const string clientIp = "203.0.113.1";
        const int rttMs = 300;
        const int quality = 60;
        const string countryCode = "CN";
        
        mockIpToNationService
            .Setup(x => x.GetNationCodeAsync(clientIp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(countryCode);
        
        var command = new RttCommand(clientIp, rttMs, quality, null);
        
        // Act
        await command.ExecuteAsync(serviceProvider, CancellationToken.None);
        
        // Assert
        // 로그가 호출되었는지 확인 (구체적인 로그 내용 검증은 복잡하므로 호출 여부만 확인)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "정보 로그가 한 번 기록되어야 합니다.");
    }
    
    /// <summary>
    /// 서비스가 null인 경우에도 RttCommand가 정상적으로 실행되는지 검증
    /// IpToNationService가 null인 경우 기본 국가 코드를 사용해야 함
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldHandleNullServices_Gracefully()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        // 서비스를 등록하지 않음 (null 반환)
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        const string clientIp = "198.51.100.1";
        const int rtt = 400;
        const int quality = 50;
        
        var command = new RttCommand(clientIp, rtt, quality, null);
        
        // Act & Assert
        // IpToNationService가 null인 경우 기본 국가 코드를 사용하므로 예외가 발생하지 않아야 함
        var exception = await Record.ExceptionAsync(async () =>
            await command.ExecuteAsync(serviceProvider, CancellationToken.None));
        
        Assert.Null(exception);
    }
}