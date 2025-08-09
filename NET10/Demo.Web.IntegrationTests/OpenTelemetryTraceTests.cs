using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// OpenTelemetry 트레이스 기능에 대한 통합 테스트
/// </summary>
public class OpenTelemetryTraceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OpenTelemetryTraceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HttpRequest_ShouldGenerateTrace()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/test/logging");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // 트레이스가 생성되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty();
        
        // ASP.NET Core 자동 계측으로 생성된 활동 확인
        var httpActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");
        
        httpActivity.Should().NotBeNull();
        httpActivity!.DisplayName.Should().Contain("GET");
        httpActivity.Tags.Should().Contain(tag => tag.Key == "http.request.method" && tag.Value == "GET");
    }

    [Fact]
    public async Task HttpRequest_ShouldIncludeCorrectTags()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/test/logging");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var httpActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");

        httpActivity.Should().NotBeNull();
        
        // HTTP 관련 태그 확인
        var tags = httpActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
        
        tags.Should().ContainKey("http.request.method");
        tags.Should().ContainKey("url.path");
        tags.Should().ContainKey("http.response.status_code");
        tags["http.request.method"].Should().Be("GET");
        tags["http.response.status_code"].Should().Be("204");
    }

    [Fact]
    public async Task CustomActivity_ShouldBeCreatedWithCorrectTags()
    {
        // Arrange
        _factory.ClearExportedData();
        
        // Act
        var response = await _client.PostAsync("/api/user/create", 
            new StringContent("{\"email\":\"test@example.com\",\"name\":\"Test User\",\"password\":\"password123\"}", 
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // 사용자 정의 활동 확인
        var customActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web" && 
                                a.DisplayName.Contains("UserCreate"));

        customActivity.Should().NotBeNull();
        
        // 사용자 정의 태그 확인
        var tags = customActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags.Should().ContainKey("operation.type");
        tags.Should().ContainKey("user.email");
        tags["operation.type"].Should().Be("UserCreate");
        tags["user.email"].Should().Be("test@example.com");
    }

    [Fact]
    public async Task ErrorRequest_ShouldSetActivityStatusToError()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var httpActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");

        httpActivity.Should().NotBeNull();
        httpActivity!.Status.Should().Be(ActivityStatusCode.Error);
        
        // 에러 태그 확인
        var tags = httpActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["http.response.status_code"].Should().Be("404");
    }

    [Fact]
    public async Task MultipleRequests_ShouldGenerateMultipleTraces()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/test/logging"));
        }
        
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.NoContent));
        
        // 5개의 HTTP 활동이 생성되었는지 확인
        var httpActivities = _factory.ExportedActivities
            .Where(a => a.Source.Name == "Microsoft.AspNetCore")
            .ToList();

        httpActivities.Should().HaveCount(5);
        
        // 각 활동이 고유한 TraceId를 가지는지 확인
        var traceIds = httpActivities.Select(a => a.TraceId).Distinct().ToList();
        traceIds.Should().HaveCount(5);
    }

    [Fact]
    public async Task LiteBusCommand_ShouldGenerateCustomTrace()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"test@example.com\",\"name\":\"Test User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // LiteBus 명령 처리에 대한 사용자 정의 활동 확인
        var liteBusActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web" && 
                                a.DisplayName.Contains("Command"));

        liteBusActivity.Should().NotBeNull();
        
        // 명령 관련 태그 확인
        var tags = liteBusActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags.Should().ContainKey("command.type");
        tags.Should().ContainKey("command.assembly");
    }
}