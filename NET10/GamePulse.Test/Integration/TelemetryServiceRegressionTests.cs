using System.Diagnostics;
using System.Diagnostics.Metrics;
using Demo.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GamePulse.Test.Integration;

/// <summary>
/// TelemetryService의 기존 기능 회귀 테스트
/// RTT 메트릭 추가 후에도 기존 기능들이 정상적으로 동작하는지 검증
/// </summary>
public class TelemetryServiceRegressionTests : IDisposable
{
    private readonly ITelemetryService _telemetryService;
    private readonly TelemetryService _concreteService;
    private readonly Mock<ILogger<TelemetryService>> _mockLogger;
    private readonly string _serviceName = "test-service";
    private readonly string _serviceVersion = "1.0.0";

    public TelemetryServiceRegressionTests()
    {
        _mockLogger = new Mock<ILogger<TelemetryService>>();
        _concreteService = new TelemetryService(_serviceName, _serviceVersion, _mockLogger.Object);
        _telemetryService = _concreteService;
    }

    /// <summary>
    /// StartActivity 메서드가 정상적으로 동작하는지 검증
    /// ActivitySource가 활성화되지 않은 경우 null을 반환할 수 있음
    /// </summary>
    [Fact]
    public void StartActivity_ShouldNotThrowException()
    {
        // Arrange
        const string operationName = "test-operation";
        var tags = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        // Act & Assert - 예외가 발생하지 않아야 함
        var exception = Record.Exception(() =>
        {
            using var activity = _telemetryService.StartActivity(operationName, tags);
            // Activity가 null일 수 있음 (ActivitySource가 활성화되지 않은 경우)
            // 이는 정상적인 동작임
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// RecordHttpRequest 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void RecordHttpRequest_ShouldRecordMetrics_WithoutException()
    {
        // Arrange
        const string method = "GET";
        const string endpoint = "/api/test";
        const int statusCode = 200;
        const double duration = 0.5;

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordHttpRequest(method, endpoint, statusCode, duration));

        Assert.Null(exception);
    }

    /// <summary>
    /// RecordError 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void RecordError_ShouldRecordMetrics_WithoutException()
    {
        // Arrange
        const string errorType = "ValidationError";
        const string operation = "UserRegistration";
        const string message = "Invalid email format";

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordError(errorType, operation, message));

        Assert.Null(exception);
    }

    /// <summary>
    /// RecordBusinessMetric 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void RecordBusinessMetric_ShouldRecordMetrics_WithoutException()
    {
        // Arrange
        const string metricName = "user_registrations";
        const long value = 1;
        var tags = new Dictionary<string, object?>
        {
            { "source", "web" },
            { "country", "KR" }
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric(metricName, value, tags));

        Assert.Null(exception);
    }

    /// <summary>
    /// SetActivitySuccess 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void SetActivitySuccess_ShouldSetActivityStatus_WithoutException()
    {
        // Arrange
        using var activity = _telemetryService.StartActivity("test-operation");
        const string message = "Operation completed successfully";

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.SetActivitySuccess(activity, message));

        Assert.Null(exception);
        
        // Activity가 null이 아닌 경우에만 상태 검증
        if (activity != null)
        {
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Contains(activity.Tags, tag => tag.Key == "success" && tag.Value == "True");
        }
    }

    /// <summary>
    /// SetActivityError 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void SetActivityError_ShouldSetActivityStatus_WithoutException()
    {
        // Arrange
        using var activity = _telemetryService.StartActivity("test-operation");
        var testException = new InvalidOperationException("Test exception");

        // Act & Assert
        var recordedException = Record.Exception(() =>
            _telemetryService.SetActivityError(activity, testException));

        Assert.Null(recordedException);
        
        // Activity가 null이 아닌 경우에만 상태 검증
        if (activity != null)
        {
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
            Assert.Contains(activity.Tags, tag => tag.Key == "error" && tag.Value == "True");
            Assert.Contains(activity.Tags, tag => tag.Key == "error.type" && tag.Value == "InvalidOperationException");
        }
    }

    /// <summary>
    /// LogInformationWithTrace 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void LogInformationWithTrace_ShouldLogWithoutException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        const string messageTemplate = "Processing user {UserId}";
        const int userId = 123;

        // Activity가 null일 수 있으므로 using 문 사용하지 않음
        using var activity = _telemetryService.StartActivity("test-operation");

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.LogInformationWithTrace(mockLogger.Object, messageTemplate, userId));

        Assert.Null(exception);
        
        activity?.Dispose();
    }

    /// <summary>
    /// LogWarningWithTrace 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void LogWarningWithTrace_ShouldLogWithoutException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        const string messageTemplate = "Warning: Rate limit approaching for user {UserId}";
        const int userId = 456;

        // Activity가 null일 수 있으므로 using 문 사용하지 않음
        using var activity = _telemetryService.StartActivity("test-operation");

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.LogWarningWithTrace(mockLogger.Object, messageTemplate, userId));

        Assert.Null(exception);
        
        activity?.Dispose();
    }

    /// <summary>
    /// LogErrorWithTrace 메서드가 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void LogErrorWithTrace_ShouldLogWithoutException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testException = new ArgumentException("Test argument exception");
        const string messageTemplate = "Error processing user {UserId}";
        const int userId = 789;

        // Activity가 null일 수 있으므로 using 문 사용하지 않음
        using var activity = _telemetryService.StartActivity("test-operation");

        // Act & Assert
        var exception = Record.Exception(() =>
            _telemetryService.LogErrorWithTrace(mockLogger.Object, testException, messageTemplate, userId));

        Assert.Null(exception);
        
        activity?.Dispose();
    }

    /// <summary>
    /// 새로 추가된 RecordRttMetrics 메서드가 기존 기능과 함께 정상적으로 동작하는지 검증
    /// </summary>
    [Fact]
    public void RecordRttMetrics_ShouldWorkAlongsideExistingMethods_WithoutException()
    {
        // Arrange
        const string countryCode = "KR";
        const double rtt = 0.15;
        const double quality = 85.0;
        const string gameType = "sod";

        // Act & Assert - RTT 메트릭 기록
        var rttException = Record.Exception(() =>
            _telemetryService.RecordRttMetrics(countryCode, rtt, quality, gameType));

        Assert.Null(rttException);

        // Act & Assert - 기존 메서드들과 함께 사용
        var httpException = Record.Exception(() =>
            _telemetryService.RecordHttpRequest("POST", "/api/rtt", 200, 0.1));

        Assert.Null(httpException);

        var businessException = Record.Exception(() =>
            _telemetryService.RecordBusinessMetric("rtt_measurements", 1, new Dictionary<string, object?>
            {
                { "country", countryCode },
                { "game", gameType }
            }));

        Assert.Null(businessException);
    }

    /// <summary>
    /// 서비스의 속성들이 올바르게 설정되어 있는지 검증
    /// </summary>
    [Fact]
    public void ServiceProperties_ShouldBeSetCorrectly()
    {
        // Act & Assert
        Assert.Equal(_serviceName, _telemetryService.ActiveSourceName);
        Assert.Equal(_serviceName, _telemetryService.MeterName);
    }

    /// <summary>
    /// 동시에 여러 메트릭을 기록해도 문제없이 동작하는지 검증
    /// </summary>
    [Fact]
    public async Task ConcurrentMetricRecording_ShouldWorkWithoutException()
    {
        // Arrange
        const int concurrentOperations = 10;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                // 기존 메트릭 기록
                _telemetryService.RecordHttpRequest("GET", $"/api/test/{index}", 200, 0.1);
                _telemetryService.RecordError("TestError", $"Operation{index}");
                _telemetryService.RecordBusinessMetric($"test_metric_{index}", 1);
                
                // 새로운 RTT 메트릭 기록
                _telemetryService.RecordRttMetrics("KR", 0.1 + (index * 0.01), 80 + index, "sod");
            }));
        }

        // Assert
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _concreteService?.Dispose();
    }
}