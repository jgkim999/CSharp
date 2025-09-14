using Demo.Admin.Models;
using RestSharp;
using System.Text.Json;

namespace Demo.Admin.Services;

public interface IUserService
{
    Task<(bool Success, string Message)> CreateUserAsync(UserCreateModel model);
}

public class UserService : IUserService
{
    private readonly RestClient _restClient;
    private readonly ILogger<UserService> _logger;

    public UserService(RestClient restClient, ILogger<UserService> logger)
    {
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(UserCreateModel model)
    {
        try
        {
            var request = new RestRequest("api/user/create", Method.Post);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // Demo.Web UserCreateEndPointV1에 맞는 요청 객체 구성
            var requestObject = new
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password
            };

            request.AddJsonBody(requestObject);

            _logger.LogInformation("사용자 생성 API 호출 시작: {Email}", model.Email);

            var response = await _restClient.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                _logger.LogInformation("사용자 생성 성공: {Email}", model.Email);
                return (true, "사용자가 성공적으로 생성되었습니다.");
            }
            else
            {
                var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
                
                if (!string.IsNullOrEmpty(response.Content))
                {
                    try
                    {
                        // 새로운 ErrorResponse 형식 파싱 시도
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Content, options);
                        
                        // ErrorResponse 형식인지 확인
                        if (errorResponse.TryGetProperty("message", out var messageProperty) && 
                            messageProperty.ValueKind == JsonValueKind.String)
                        {
                            var userFriendlyMessage = messageProperty.GetString();
                            if (!string.IsNullOrEmpty(userFriendlyMessage))
                            {
                                errorMessage = userFriendlyMessage;
                            }
                        }
                        // 기존 FastEndpoints errors 배열 형식 지원
                        else if (errorResponse.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                        {
                            var errorMessages = errors.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.String)
                                .Select(e => e.GetString())
                                .Where(s => !string.IsNullOrEmpty(s));
                            
                            if (errorMessages.Any())
                            {
                                errorMessage = string.Join(", ", errorMessages);
                            }
                        }
                        // 일반 JSON 객체에서 message 또는 error 속성 찾기
                        else if (errorResponse.TryGetProperty("error", out var errorProperty) && 
                                 errorProperty.ValueKind == JsonValueKind.String)
                        {
                            var errorText = errorProperty.GetString();
                            if (!string.IsNullOrEmpty(errorText))
                            {
                                errorMessage = errorText;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // JSON 파싱 실패 시 원본 응답 내용 사용 (하지만 너무 길면 자름)
                        var content = response.Content;
                        if (content.Length > 200)
                        {
                            content = content.Substring(0, 200) + "...";
                        }
                        errorMessage = content;
                    }
                }

                _logger.LogError("사용자 생성 실패: {Email}, 상태코드: {StatusCode}, 오류: {Error}", 
                    model.Email, response.StatusCode, errorMessage);
                
                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 생성 중 예외 발생: {Email}", model.Email);
            return (false, $"네트워크 오류가 발생했습니다: {ex.Message}");
        }
    }
}