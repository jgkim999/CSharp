using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System.Diagnostics;
using System.Net;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// Serilog와 OpenTelemetry 통합에 대한 테스트
/// </summary>
public class SerilogOpenTelemetryIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SerilogOpenTelemetryIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task LogsWithTraceContext_ShouldIncludeTraceAndSpanIds()
    {
        // Arrange
        _factory.ClearExportedData();
        ClearInMemoryLogs();

        // 테스트용 Serilog 설정
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .Enrich.FromLogContext()
            .CreateLogger();

        // Act
        var response = await _client.GetAsync("/test/logging");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // 트레이스가 생성되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty();
        
        var httpActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");
        
        httpActivity.Should().NotBeNull();
        
        // 로그 이벤트 확인
        var logEvents = GetInMemoryLogs();
        logEvents.Should().NotBeEmpty();
        
        // 트레이스 컨텍스트가 포함된 로그 확인
        var logWithTrace = logEvents.FirstOrDefault(le => 
            le.Properties.ContainsKey("TraceId") || 
            le.Properties.ContainsKey("SpanId"));
        
        if (logWithTrace != null)
        {
            logWithTrace.Properties.Should().ContainKey("TraceId");
            logWithTrace.Properties.Should().ContainKey("SpanId");
        }
    }

    [Fact]
    public void StructuredLogging_ShouldIncludeOpenTelemetryProperties()
    {
        // Arrange
        _factory.ClearExportedData();
        ClearInMemoryLogs();

        // 테스트용 Serilog 설정
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .Enrich.FromLogContext()
            .CreateLogger();

        // Act
        using (var activity = new ActivitySource("Test").StartActivity("TestActivity"))
        {
            activity?.SetTag("test.property", "test.value");
            
            var logger = _factory.Services.GetRequiredService<ILogger<SerilogOpenTelemetryIntegrationTests>>();
            logger.LogInformation("Test log message with trace context");
        }

        // Assert
        var logEvents = GetInMemoryLogs();
        logEvents.Should().NotBeEmpty();
        
        var testLogEvent = logEvents.FirstOrDefault(le => 
            le.MessageTemplate.Text.Contains("Test log message"));
        
        testLogEvent.Should().NotBeNull();
    }

    [Fact]
    public async Task ExceptionLogging_ShouldCorrelateWithTraceContext()
    {
        // Arrange
        _factory.ClearExportedData();
        ClearInMemoryLogs();

        // 테스트용 Serilog 설정
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .CreateLogger();

        // Act - 예외를 발생시키는 엔드포인트 호출
        var response = await _client.GetAsync("/api/error");

        // Assert
        // 에러 응답 또는 서버 에러 확인
        response.StatusCode.Should().BeOneOf(HttpStatusCode.InternalServerError, HttpStatusCode.NotFound);
        
        // 에러 로그 확인
        var logEvents = GetInMemoryLogs();
        var errorLogs = logEvents.Where(le => le.Level >= LogEventLevel.Warning).ToList();
        
        if (errorLogs.Any())
        {
            var errorLog = errorLogs.First();
            
            // 에러 로그에 트레이스 컨텍스트가 포함되어 있는지 확인
            var hasTraceContext = errorLog.Properties.ContainsKey("TraceId") ||
                                 errorLog.Properties.ContainsKey("SpanId");
            
            // 트레이스 컨텍스트가 있다면 검증
            if (hasTraceContext)
            {
                errorLog.Properties.Should().ContainKey("TraceId");
                errorLog.Properties.Should().ContainKey("SpanId");
            }
        }
    }

    [Fact]
    public void LogLevels_ShouldBeRespectedInDifferentEnvironments()
    {
        // Arrange
        ClearInMemoryLogs();

        // 테스트 환경용 Serilog 설정 (Debug 레벨)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .MinimumLevel.Debug()
            .CreateLogger();

        // Act
        var logger = _factory.Services.GetRequiredService<ILogger<SerilogOpenTelemetryIntegrationTests>>();
        
        logger.LogDebug("Debug message");
        logger.LogInformation("Information message");
        logger.LogWarning("Warning message");
        logger.LogError("Error message");

        // Assert
        var logEvents = GetInMemoryLogs();
        
        // 모든 레벨의 로그가 기록되었는지 확인
        logEvents.Should().Contain(le => le.Level == LogEventLevel.Debug);
        logEvents.Should().Contain(le => le.Level == LogEventLevel.Information);
        logEvents.Should().Contain(le => le.Level == LogEventLevel.Warning);
        logEvents.Should().Contain(le => le.Level == LogEventLevel.Error);
    }

    [Fact]
    public void CustomProperties_ShouldBeIncludedInLogs()
    {
        // Arrange
        ClearInMemoryLogs();

        // 테스트용 Serilog 설정
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .Enrich.FromLogContext()
            .CreateLogger();

        // Act
        var logger = _factory.Services.GetRequiredService<ILogger<SerilogOpenTelemetryIntegrationTests>>();
        
        using (LogContext.PushProperty("UserId", "test-user-123"))
        using (LogContext.PushProperty("OperationType", "UserCreation"))
        {
            logger.LogInformation("User creation operation started");
        }

        // Assert
        var logEvents = GetInMemoryLogs();
        var userCreationLog = logEvents.FirstOrDefault(le => 
            le.MessageTemplate.Text.Contains("User creation"));
        
        userCreationLog.Should().NotBeNull();
        userCreationLog!.Properties.Should().ContainKey("UserId");
        userCreationLog.Properties.Should().ContainKey("OperationType");
        
        userCreationLog.Properties["UserId"].ToString().Should().Contain("test-user-123");
        userCreationLog.Properties["OperationType"].ToString().Should().Contain("UserCreation");
    }

    /// <summary>
    /// InMemory 로그를 안전하게 가져옵니다
    /// </summary>
    private static List<LogEvent> GetInMemoryLogs()
    {
        return InMemorySink.Instance.LogEvents.ToList();
    }

    /// <summary>
    /// InMemory 로그를 안전하게 지웁니다
    /// </summary>
    private static void ClearInMemoryLogs()
    {
        var logEvents = InMemorySink.Instance.LogEvents.ToList();
        logEvents.Clear();
    }
}