using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Demo.Web.Endpoints.User;
using Demo.Web.Configs;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// Rate Limiting 통합 테스트 클래스
/// UserCreateEndpointV1과 Rate Limiting의 통합 동작을 검증합니다.
/// </summary>
public class RateLimitingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// RateLimitingIntegrationTests 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="factory">테스트용 웹 애플리케이션 팩토리</param>
    public RateLimitingIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Rate Limiting이 적용되지 않은 정상적인 요청이 성공하는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_WithinRateLimit_ShouldReturnSuccess()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/user/create", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Rate Limit을 초과한 요청이 429 상태 코드를 반환하는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_ExceedingRateLimit_ShouldReturn429()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(request);

        // Act - Rate Limit (기본값: 10회)을 초과하여 요청
        var responses = new List<HttpResponse>();
        for (int i = 0; i < 12; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/user/create", content);
            responses.Add(new HttpResponse
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? "")
            });
        }

        // Assert
        // 처음 10개 요청은 성공해야 함
        for (int i = 0; i < 10; i++)
        {
            responses[i].StatusCode.Should().Be(HttpStatusCode.OK, 
                $"Request {i + 1} should succeed within rate limit");
        }

        // 11번째와 12번째 요청은 Rate Limit 초과로 429 반환해야 함
        responses[10].StatusCode.Should().Be(HttpStatusCode.TooManyRequests, 
            "Request 11 should be rate limited");
        responses[11].StatusCode.Should().Be(HttpStatusCode.TooManyRequests, 
            "Request 12 should be rate limited");

        // Retry-After 헤더가 포함되어야 함
        responses[10].Headers.Should().ContainKey("Retry-After", 
            "Rate limited response should include Retry-After header");
    }

    /// <summary>
    /// X-Forwarded-For 헤더를 통한 IP 식별이 올바르게 작동하는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_WithXForwardedForHeader_ShouldIdentifyClientCorrectly()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(request);

        // Act - 첫 번째 IP로 Rate Limit까지 요청
        var firstIpResponses = new List<HttpStatusCode>();
        for (int i = 0; i < 11; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.100");
            
            var response = await client.PostAsync("/api/user/create", content);
            firstIpResponses.Add(response.StatusCode);
        }

        // Act - 두 번째 IP로 요청 (독립적인 Rate Limit이어야 함)
        var secondIpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var secondIpClient = _factory.CreateClient();
        secondIpClient.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.200");
        
        var secondIpResponse = await secondIpClient.PostAsync("/api/user/create", secondIpContent);

        // Assert
        // 첫 번째 IP: 처음 10개는 성공, 11번째는 Rate Limit
        for (int i = 0; i < 10; i++)
        {
            firstIpResponses[i].Should().Be(HttpStatusCode.OK, 
                $"First IP request {i + 1} should succeed");
        }
        firstIpResponses[10].Should().Be(HttpStatusCode.TooManyRequests, 
            "First IP request 11 should be rate limited");

        // 두 번째 IP: 독립적인 Rate Limit으로 성공해야 함
        secondIpResponse.StatusCode.Should().Be(HttpStatusCode.OK, 
            "Second IP should have independent rate limit");
    }

    /// <summary>
    /// 다른 IP 주소들이 독립적인 Rate Limit을 가지는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_DifferentIPs_ShouldHaveIndependentRateLimits()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(request);
        var ipAddresses = new[] { "10.0.0.1", "10.0.0.2", "10.0.0.3" };

        // Act & Assert - 각 IP별로 독립적인 Rate Limit 확인
        foreach (var ipAddress in ipAddresses)
        {
            var responses = new List<HttpStatusCode>();
            
            // 각 IP로 11번 요청
            for (int i = 0; i < 11; i++)
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
                
                var response = await client.PostAsync("/api/user/create", content);
                responses.Add(response.StatusCode);
            }

            // 각 IP별로 처음 10개는 성공, 11번째는 Rate Limit이어야 함
            for (int i = 0; i < 10; i++)
            {
                responses[i].Should().Be(HttpStatusCode.OK, 
                    $"IP {ipAddress} request {i + 1} should succeed");
            }
            responses[10].Should().Be(HttpStatusCode.TooManyRequests, 
                $"IP {ipAddress} request 11 should be rate limited");
        }
    }

    /// <summary>
    /// Rate Limiting 설정이 비활성화된 경우 제한이 적용되지 않는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_WhenRateLimitingDisabled_ShouldNotApplyRateLimit()
    {
        // Arrange - Rate Limiting을 비활성화한 팩토리 생성
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // RateLimitConfig를 비활성화 상태로 교체
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RateLimitConfig));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var disabledConfig = new RateLimitConfig
                    {
                        UserCreateEndpoint = new UserCreateEndpointConfig
                        {
                            Enabled = false,
                            HitLimit = 10,
                            DurationSeconds = 60
                        }
                    };
                    services.AddSingleton(disabledConfig);
                });
                builder.UseEnvironment("Testing");
            });

        var client = factory.CreateClient();

        var request = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "test@example.com",
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(request);

        // Act - Rate Limit을 초과하는 요청 수행 (15회)
        var responses = new List<HttpStatusCode>();
        for (int i = 0; i < 15; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            responses.Add(response.StatusCode);
        }

        // Assert - 모든 요청이 성공해야 함 (Rate Limiting이 비활성화되었으므로)
        responses.Should().AllSatisfy(statusCode => 
            statusCode.Should().Be(HttpStatusCode.OK, 
                "All requests should succeed when rate limiting is disabled"));

        // Cleanup
        factory.Dispose();
    }

    /// <summary>
    /// 잘못된 요청 데이터로 인한 검증 오류가 Rate Limit에 포함되는지 테스트합니다.
    /// </summary>
    [Fact]
    public async Task UserCreateEndpoint_ValidationErrors_ShouldStillCountTowardsRateLimit()
    {
        // Arrange - 잘못된 요청 데이터 (이메일 형식 오류)
        var invalidRequest = new UserCreateRequest
        {
            Name = "TestUser",
            Email = "invalid-email", // 잘못된 이메일 형식
            Password = "password123"
        };

        var json = JsonSerializer.Serialize(invalidRequest);

        // Act - 잘못된 요청을 Rate Limit을 초과하여 전송
        var responses = new List<HttpResponse>();
        for (int i = 0; i < 12; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/user/create", content);
            responses.Add(new HttpResponse
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? "")
            });
        }

        // Assert
        // 처음 10개 요청은 검증 오류로 400 반환
        for (int i = 0; i < 10; i++)
        {
            responses[i].StatusCode.Should().Be(HttpStatusCode.BadRequest, 
                $"Request {i + 1} should return validation error");
        }

        // 11번째와 12번째 요청은 Rate Limit 초과로 429 반환 (검증 오류도 Rate Limit에 포함됨)
        responses[10].StatusCode.Should().Be(HttpStatusCode.TooManyRequests, 
            "Request 11 should be rate limited even with validation errors");
        responses[11].StatusCode.Should().Be(HttpStatusCode.TooManyRequests, 
            "Request 12 should be rate limited even with validation errors");
    }
}

/// <summary>
/// HTTP 응답 정보를 담는 헬퍼 클래스
/// </summary>
public class HttpResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}