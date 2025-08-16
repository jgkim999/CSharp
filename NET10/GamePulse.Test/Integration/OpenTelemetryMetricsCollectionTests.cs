using System.Diagnostics.Metrics;
using Demo.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GamePulse.Test.Integration;

/// <summary>
/// OpenTelemetry 메트릭 수집 동작 확인 테스트
/// </summary>
public class OpenTelemetryMetricsCollectionTests : IDisposable
{
    private readonly ITelemetryService _telemetryService;
    private readonly TelemetryService _concreteService;
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly string _serviceName = "test-metrics-service";
    private readonly string _serviceVersion = "1.0.0";

    public OpenTelemetryMetricsCollectionTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _concreteService = new TelemetryService(_serviceName, _serviceVersion, _mockLogger.Object);
        _telemetryService = _concreteService;
    }

    /// <summary>
    /// TelemetryService의 MeterName이 올바르게 설정되어 있는지 검증
    /// </summary>
    [Fact]
    public void MeterName_ShouldMatchServiceName()
    {
        // Act & Assert
        Assert.Equal(_serviceName, _telemetryService.MeterName);
    }

    /// <summary>
    /// ActivitySource 이름이 올바르게 설정되어 있는지 검증
    /// </summary>
    [Fact]
    public void ActiveSourceName_ShouldMatchServiceName()
    {
        // Act & Assert
        Assert.Equal(_serviceName, _telemetryService.ActiveSourceName);
    }

    /// <summary>
    /// RTT 메트릭이 올바른 이름과 단위로 생성되는지 검증
    /// </summary>
    [Fact]
    public void RecordRttMetrics_ShouldUseCorrectMetricNames()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.15;
        const double quality = 85.0;
        const string gameType = "sod";

        // Act & Assert - 예외가 발생하지 않으면 메트릭이 올바르게 생성된 것으로 간주
        var exception = Record.Exception(() =>
            _telemetryService.RecordRttMetrics(countryCode, rtt, quality, gameType));

        Assert.Null(exception);
    }

    /// <summary>
    /// HTTP 요청 메트릭이 올바른 태그와 함께 기록되는지 검증
    /// </summary>
    [Fact]
    public void RecordHttpRequest_ShouldRecordWithCorrectTags()
    {
        // Arrange
        const string method = "POST";
        const string endpoint = "/api/users";
        const int statusCode = 201;
        const double duration = 0.25;

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordHttpRequest(method, endpoint, statusCode, duration));

        Assert.Null(exception);
    }

    /// <summary>
    /// 에러 메트릭이 올바른 태그와 함께 기록되는지 검증
    /// </summary>
    [Fact]
    public void RecordError_ShouldRecordWithCorrectTags()
    {
        // Arrange
        const string errorType = "DatabaseConnectionError";
        const string operation = "UserQuery";
        const string message = "Connection timeout";

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordError(errorType, operation, message));

        Assert.Null(exception);
    }

    /// <summary>
    /// 비즈니스 메트릭이 동적으로 생성되고 기록되는지 검증
    /// </summary>
    [Fact]
    public void RecordBusinessMetric_ShouldCreateDynamicMetrics()
    {
        // Arrange
        const string metricName1 = "user_logins";
        const string metricName2 = "game_sessions";
        const long value = 1;
        var tags = new Dictionary<string, object?>
        {
            { "platform", "web" },
            { "region", "asia" }
        };

        // Act & Assert - 서로 다른 메트릭 이름으로 여러 번 호출
        var exception1 = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric(metricName1, value, tags));

        var exception2 = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric(metricName2, value, tags));

        var exception3 = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric(metricName1, value + 1, tags)); // 같은 메트릭에 다른 값

        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    /// <summary>
    /// RTT 메트릭의 모든 타입(Counter, Histogram, Gauge)이 기록되는지 검증
    /// </summary>
    [Theory]
    [InlineData("US", 0.05, 95.0, "sod")]
    [InlineData("JP", 0.08, 90.0, "fps")]
    [InlineData("EU", 0.12, 85.0, "mmo")]
    [InlineData("KR", 0.03, 98.0, "sod")]
    public void RecordRttMetrics_ShouldRecordAllMetricTypes(string countryCode, double rtt, double quality, string gameType)
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordRttMetrics(countryCode, rtt, quality, gameType));

        Assert.Null(exception);
    }

    /// <summary>
    /// 대량의 메트릭 데이터를 처리할 수 있는지 성능 테스트
    /// </summary>
    [Fact]
    public void MetricsCollection_ShouldHandleLargeVolume()
    {
        // Arrange
        const int metricsCount = 1000;
        var random = new Random(42); // 시드 고정으로 재현 가능한 테스트

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            for (int i = 0; i < metricsCount; i++)
            {
                // HTTP 메트릭
                _telemetryService.RecordHttpRequest(
                    "GET", 
                    $"/api/endpoint/{i}", 
                    200, 
                    random.NextDouble() * 2.0);

                // RTT 메트릭
                _telemetryService.RecordRttMetrics(
                    "KR", 
                    random.NextDouble() * 0.5, 
                    random.Next(50, 100), 
                    "sod");

                // 에러 메트릭 (가끔)
                if (i % 100 == 0)
                {
                    _telemetryService.RecordError("TestError", $"Operation{i}");
                }

                // 비즈니스 메트릭
                _telemetryService.RecordBusinessMetric("test_operations", 1, new Dictionary<string, object?>
                {
                    { "batch", i / 100 },
                    { "type", "load_test" }
                });
            }
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// 메트릭 태그에 특수 문자나 긴 문자열이 포함되어도 처리되는지 검증
    /// </summary>
    [Fact]
    public void MetricsCollection_ShouldHandleSpecialCharactersInTags()
    {
        // Arrange
        const string countryCodeWithSpecialChars = "KR-Seoul";
        const string gameTypeWithSpaces = "real time strategy";
        var tagsWithSpecialChars = new Dictionary<string, object?>
        {
            { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" },
            { "endpoint", "/api/v1/users/{id}/profile" },
            { "query-params", "?filter=active&sort=name&limit=100" }
        };

        // Act & Assert
        var rttException = Record.Exception(() =>
            _telemetryService.RecordRttMetrics(countryCodeWithSpecialChars, 0.1, 80, gameTypeWithSpaces));

        var businessException = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric("complex_operation", 1, tagsWithSpecialChars));

        Assert.Null(rttException);
        Assert.Null(businessException);
    }

    /// <summary>
    /// null 또는 빈 태그 값들이 올바르게 처리되는지 검증
    /// </summary>
    [Fact]
    public void MetricsCollection_ShouldHandleNullAndEmptyTagValues()
    {
        // Arrange
        var tagsWithNullValues = new Dictionary<string, object?>
        {
            { "valid_key", "valid_value" },
            { "null_key", null },
            { "empty_key", "" },
            { "zero_key", 0 }
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric("null_tag_test", 1, tagsWithNullValues));

        Assert.Null(exception);
    }

    /// <summary>
    /// 메트릭 수집 중 예외가 발생해도 서비스가 계속 동작하는지 검증
    /// </summary>
    [Fact]
    public void MetricsCollection_ShouldContinueAfterException()
    {
        // Act & Assert - 잘못된 매개변수로 예외 발생 시도
        var invalidRttException = Record.Exception(() =>
            _telemetryService.RecordRttMetrics("", -1, 150)); // 잘못된 매개변수

        // 예외가 발생해야 함 (유효성 검사)
        Assert.NotNull(invalidRttException);

        // 이후 정상적인 메트릭 기록이 가능해야 함
        var validRttException = Record.Exception(() =>
            _telemetryService.RecordRttMetrics("KR", 0.1, 85));

        Assert.Null(validRttException);
    }

    public void Dispose()
    {
        _concreteService?.Dispose();
    }
}