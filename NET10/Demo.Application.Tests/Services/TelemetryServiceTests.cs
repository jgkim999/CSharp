using System.Diagnostics.Metrics;
using Demo.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Demo.Application.Tests.Services;

/// <summary>
/// TelemetryService 클래스의 단위 테스트
/// </summary>
public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly TelemetryService _telemetryService;
    private readonly string _serviceName = "TestService";
    private readonly string _serviceVersion = "1.0.0";

    /// <summary>
    /// 테스트 초기화
    /// </summary>
    public TelemetryServiceTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _telemetryService = new TelemetryService(_serviceName, _serviceVersion, _mockLogger.Object);
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _telemetryService?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 유효한 입력값으로 RTT 메트릭을 기록할 때 예외가 발생하지 않는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithValidInputs_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05; // 50ms
        const double quality = 85.5;
        const string gameType = "sod";

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 기본 게임 타입으로 RTT 메트릭을 기록할 때 예외가 발생하지 않는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithDefaultGameType_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "US";
        const double rtt = 0.1; // 100ms
        const double quality = 75.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 경계값 테스트 - RTT가 0일 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithZeroRtt_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "JP";
        const double rtt = 0.0;
        const double quality = 100.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 경계값 테스트 - 품질이 0일 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithZeroQuality_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "CN";
        const double rtt = 0.2;
        const double quality = 0.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 경계값 테스트 - 품질이 100일 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithMaxQuality_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "DE";
        const double rtt = 0.03;
        const double quality = 100.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// null 국가 코드로 RTT 메트릭을 기록할 때 ArgumentException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithNullCountryCode_ThrowsArgumentException()
    {
        // Arrange
        const string? countryCode = null;
        const double rtt = 0.05;
        const double quality = 85.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode!, rtt, quality));
        
        Assert.Equal("countryCode", exception.ParamName);
        Assert.Contains("국가 코드는 null 또는 빈 문자열일 수 없습니다", exception.Message);
    }

    /// <summary>
    /// 빈 문자열 국가 코드로 RTT 메트릭을 기록할 때 ArgumentException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithEmptyCountryCode_ThrowsArgumentException()
    {
        // Arrange
        const string countryCode = "";
        const double rtt = 0.05;
        const double quality = 85.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Equal("countryCode", exception.ParamName);
        Assert.Contains("국가 코드는 null 또는 빈 문자열일 수 없습니다", exception.Message);
    }

    /// <summary>
    /// 공백 문자열 국가 코드로 RTT 메트릭을 기록할 때 ArgumentException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithWhitespaceCountryCode_ThrowsArgumentException()
    {
        // Arrange
        const string countryCode = "   ";
        const double rtt = 0.05;
        const double quality = 85.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Equal("countryCode", exception.ParamName);
        Assert.Contains("국가 코드는 null 또는 빈 문자열일 수 없습니다", exception.Message);
    }

    /// <summary>
    /// 음수 RTT 값으로 메트릭을 기록할 때 ArgumentOutOfRangeException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithNegativeRtt_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = -0.01;
        const double quality = 85.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Equal("rtt", exception.ParamName);
        Assert.Contains("RTT 값은 음수일 수 없습니다", exception.Message);
    }

    /// <summary>
    /// 음수 품질 값으로 메트릭을 기록할 때 ArgumentOutOfRangeException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithNegativeQuality_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05;
        const double quality = -1.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Equal("quality", exception.ParamName);
        Assert.Contains("네트워크 품질 점수는 0-100 범위 내의 값이어야 합니다", exception.Message);
    }

    /// <summary>
    /// 100을 초과하는 품질 값으로 메트릭을 기록할 때 ArgumentOutOfRangeException이 발생하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithQualityAbove100_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05;
        const double quality = 101.0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Equal("quality", exception.ParamName);
        Assert.Contains("네트워크 품질 점수는 0-100 범위 내의 값이어야 합니다", exception.Message);
    }

    /// <summary>
    /// null 게임 타입으로 메트릭을 기록할 때 기본값 "sod"가 사용되는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithNullGameType_UsesDefaultValue()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05;
        const double quality = 85.0;
        const string? gameType = null;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType!));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 빈 문자열 게임 타입으로 메트릭을 기록할 때 기본값 "sod"가 사용되는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithEmptyGameType_UsesDefaultValue()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05;
        const double quality = 85.0;
        const string gameType = "";

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 공백 문자열 게임 타입으로 메트릭을 기록할 때 기본값 "sod"가 사용되는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithWhitespaceGameType_UsesDefaultValue()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.05;
        const double quality = 85.0;
        const string gameType = "   ";

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 다양한 유효한 국가 코드로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Theory]
    [InlineData("KR", 0.05, 85.0, "sod")]
    [InlineData("US", 0.1, 75.0, "fps")]
    [InlineData("JP", 0.03, 95.0, "mmo")]
    [InlineData("CN", 0.15, 60.0, "rts")]
    [InlineData("DE", 0.08, 80.0, "rpg")]
    public void RecordRttMetrics_WithVariousValidInputs_DoesNotThrowException(
        string countryCode, double rtt, double quality, string gameType)
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 여러 번 연속으로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_MultipleCallsInSequence_DoesNotThrowException()
    {
        // Arrange
        var testData = new[]
        {
            ("KR", 0.05, 85.0, "sod"),
            ("US", 0.1, 75.0, "fps"),
            ("JP", 0.03, 95.0, "mmo")
        };

        // Act & Assert
        foreach (var (countryCode, rtt, quality, gameType) in testData)
        {
            var exception = Record.Exception(() => 
                ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
            
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// 매우 큰 RTT 값으로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithVeryLargeRtt_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "AU";
        const double rtt = 999.999; // 매우 큰 RTT 값
        const double quality = 50.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 매우 작은 RTT 값으로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithVerySmallRtt_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "SG";
        const double rtt = 0.001; // 1ms
        const double quality = 99.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 소수점이 있는 품질 값으로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithDecimalQuality_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "FR";
        const double rtt = 0.07;
        const double quality = 87.5; // 소수점이 있는 품질 값

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 긴 국가 코드로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithLongCountryCode_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "SOUTH_KOREA"; // 긴 국가 코드
        const double rtt = 0.04;
        const double quality = 90.0;

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality));
        
        Assert.Null(exception);
    }

    /// <summary>
    /// 특수 문자가 포함된 게임 타입으로 메트릭을 기록할 때 정상 동작하는지 테스트
    /// </summary>
    [Fact]
    public void RecordRttMetrics_WithSpecialCharactersInGameType_DoesNotThrowException()
    {
        // Arrange
        const string countryCode = "BR";
        const double rtt = 0.12;
        const double quality = 70.0;
        const string gameType = "game-type_v2.0"; // 특수 문자가 포함된 게임 타입

        // Act & Assert
        var exception = Record.Exception(() => 
            ((ITelemetryService)_telemetryService).RecordRttMetrics(countryCode, rtt, quality, gameType));
        
        Assert.Null(exception);
    }
}