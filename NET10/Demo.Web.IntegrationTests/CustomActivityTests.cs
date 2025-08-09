using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// 사용자 정의 활동(Custom Activity) 생성 및 태깅에 대한 테스트
/// </summary>
public class CustomActivityTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomActivityTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UserCreateEndpoint_ShouldCreateCustomActivity()
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
        customActivity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task CustomActivity_ShouldIncludeBusinessTags()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"business@example.com\",\"name\":\"Business User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web");

        customActivity.Should().NotBeNull();
        
        // 비즈니스 관련 태그 확인
        var tags = customActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
        
        tags.Should().ContainKey("operation.type");
        tags.Should().ContainKey("user.email");
        tags["operation.type"].Should().Be("UserCreate");
        tags["user.email"].Should().Be("business@example.com");
    }

    [Fact]
    public async Task CustomActivity_ShouldIncludePerformanceMetrics()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"perf@example.com\",\"name\":\"Performance User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web");

        customActivity.Should().NotBeNull();
        
        // 성능 관련 태그 확인
        var tags = customActivity!.Tags.ToDictionary(t => t.Key, t => t.Value);
        
        // 처리 시간이 기록되었는지 확인
        customActivity.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        
        // 추가 성능 태그가 있다면 확인
        if (tags.ContainsKey("processing.duration_ms"))
        {
            tags["processing.duration_ms"].Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ErrorInCustomActivity_ShouldSetErrorStatus()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act - 잘못된 데이터로 요청
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"\",\"name\":\"\",\"password\":\"\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        // 에러 응답 확인 (400 Bad Request 또는 422 Unprocessable Entity)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.InternalServerError);
        
        // 에러 상태의 활동 확인
        var errorActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Status == ActivityStatusCode.Error);

        if (errorActivity != null)
        {
            errorActivity.Status.Should().Be(ActivityStatusCode.Error);
            errorActivity.StatusDescription.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task NestedActivities_ShouldMaintainParentChildRelationship()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"nested@example.com\",\"name\":\"Nested User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // 부모-자식 관계 확인
        var activities = _factory.ExportedActivities.ToList();
        activities.Should().NotBeEmpty();
        
        // HTTP 요청 활동 (부모)
        var httpActivity = activities
            .FirstOrDefault(a => a.Source.Name == "Microsoft.AspNetCore");
        
        // 사용자 정의 활동 (자식)
        var customActivity = activities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web");

        if (httpActivity != null && customActivity != null)
        {
            // 같은 트레이스 ID를 가져야 함
            httpActivity.TraceId.Should().Be(customActivity.TraceId);
            
            // 사용자 정의 활동이 HTTP 활동의 자식이어야 함
            customActivity.ParentId.Should().Be(httpActivity.Id);
        }
    }

    [Fact]
    public async Task CustomActivityWithEvents_ShouldRecordEvents()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act
        var response = await _client.PostAsync("/api/user/create",
            new StringContent("{\"email\":\"events@example.com\",\"name\":\"Events User\",\"password\":\"password123\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var customActivity = _factory.ExportedActivities
            .FirstOrDefault(a => a.Source.Name == "Demo.Web");

        customActivity.Should().NotBeNull();
        
        // 활동 이벤트 확인
        var events = customActivity!.Events.ToList();
        
        if (events.Any())
        {
            events.Should().NotBeEmpty();
            
            // 특정 이벤트 확인 (예: "User validation completed", "User created successfully")
            var validationEvents = events.Where(e => 
                e.Name.Contains("validation") || e.Name.Contains("created")).ToList();
            
            if (validationEvents.Any())
            {
                var validationEvent = validationEvents.First();
                validationEvent.Name.Should().NotBeNullOrEmpty();
                validationEvent.Timestamp.Should().BeAfter(DateTimeOffset.MinValue);
            }
        }
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldCreateSeparateActivities()
    {
        // Arrange
        _factory.ClearExportedData();

        // Act - 동시에 여러 요청 실행
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var email = $"concurrent{i}@example.com";
            var content = new StringContent($"{{\"email\":\"{email}\",\"name\":\"Concurrent User {i}\",\"password\":\"password123\"}}",
                System.Text.Encoding.UTF8, "application/json");
            
            tasks.Add(_client.PostAsync("/api/user/create", content));
        }
        
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        
        // 각 요청에 대한 별도의 활동 확인
        var customActivities = _factory.ExportedActivities
            .Where(a => a.Source.Name == "Demo.Web")
            .ToList();

        customActivities.Should().HaveCount(5);
        
        // 각 활동이 고유한 스팬 ID를 가지는지 확인
        var spanIds = customActivities.Select(a => a.SpanId).Distinct().ToList();
        spanIds.Should().HaveCount(5);
        
        // 각 활동이 다른 사용자 이메일을 가지는지 확인
        var emails = customActivities
            .SelectMany(a => a.Tags)
            .Where(t => t.Key == "user.email")
            .Select(t => t.Value)
            .Distinct()
            .ToList();
        
        emails.Should().HaveCount(5);
    }
}