using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using System.Net;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// OpenTelemetry 메트릭 기능에 대한 통합 테스트
/// </summary>
public class OpenTelemetryMetricsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OpenTelemetryMetricsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HttpRequest_ShouldGenerateHttpMetrics()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/test/logging");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // 메트릭이 수집되었는지 확인 (약간의 지연 후)
        await Task.Delay(100);
        
        _factory.ExportedMetrics.Should().NotBeEmpty();
        
        // HTTP 요청 관련 메트릭 확인
        var httpMetrics = _factory.ExportedMetrics
            .Where(m => m.Name.Contains("http") || m.Name.Contains("request"))
            .ToList();

        httpMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MultipleRequests_ShouldIncrementRequestCount()
    {
        // Arrange
        _factory.ClearExportedData();
        const int requestCount = 10;

        // Act
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(_client.GetAsync("/test/logging"));
        }
        
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.NoContent));
        
        // 메트릭 수집 대기
        await Task.Delay(200);
        
        // 요청 카운트 메트릭 확인
        var requestCountMetrics = _factory.ExportedMetrics
            .Where(m => m.Name.Contains("request") && m.Name.Contains("count"))
            .ToList();

        // 최소한 하나의 요청 카운트 메트릭이 있어야 함
        requestCountMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CustomMetrics_ShouldBeCollected()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"test@example.com\",\"name\":\"Test User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // 메트릭 수집 대기
        await Task.Delay(200);
        
        // 사용자 정의 메트릭 확인
        var customMetrics = _factory.ExportedMetrics
            .Where(m => m.MeterName == "Demo.Web")
            .ToList();

        customMetrics.Should().NotBeEmpty();
        
        // 사용자 생성 관련 메트릭 확인
        var userCreationMetrics = customMetrics
            .Where(m => m.Name.Contains("user") || m.Name.Contains("create"))
            .ToList();

        userCreationMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RuntimeMetrics_ShouldBeCollected()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act - 여러 요청을 보내서 런타임 메트릭 생성 유도
        for (int i = 0; i < 5; i++)
        {
            await _client.GetAsync("/test/logging");
        }

        // Assert
        // 런타임 메트릭 수집 대기
        await Task.Delay(500);
        
        // .NET 런타임 메트릭 확인
        var runtimeMetrics = _factory.ExportedMetrics
            .Where(m => m.MeterName.Contains("System.Runtime") || 
                       m.Name.Contains("process") ||
                       m.Name.Contains("dotnet"))
            .ToList();

        // 런타임 메트릭이 수집되었는지 확인
        runtimeMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ErrorRequests_ShouldGenerateErrorMetrics()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // 메트릭 수집 대기
        await Task.Delay(200);
        
        // HTTP 상태 코드 메트릭 확인
        var httpStatusMetrics = _factory.ExportedMetrics
            .Where(m => m.Name.Contains("status") || m.Name.Contains("response"))
            .ToList();

        httpStatusMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ResponseTimeMetrics_ShouldBeCollected()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/test/logging");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // 메트릭 수집 대기
        await Task.Delay(200);
        
        // 응답 시간 관련 메트릭 확인
        var responseTimeMetrics = _factory.ExportedMetrics
            .Where(m => m.Name.Contains("duration") || 
                       m.Name.Contains("time") ||
                       m.Name.Contains("latency"))
            .ToList();

        responseTimeMetrics.Should().NotBeEmpty();
    }
}