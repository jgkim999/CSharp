using FastEndpoints;
using FastEndpoints.Security;
using Demo.Application.Configs;
using Demo.Application.Services;
using Demo.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Demo.Infra.Services;

/// <summary>
/// JWT 토큰 서비스 구현체
/// </summary>
public class MyTokenService : RefreshTokenService<TokenRequest, TokenResponse>
{
    private readonly IJwtRepository _jwtRepository;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// JWT 토큰 매개변수와 리프레시 토큰 엔드포인트 설정을 구성하여 MyTokenService 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="config">구성 정보</param>
    /// <param name="jwtRepository">JWT 저장소</param>
    /// <param name="telemetryService">Telemetry service</param>
    /// <exception cref="NullReferenceException">
    /// config, logger, jwtRepository 또는 "Jwt" 구성 섹션이 null인 경우 발생
    /// <summary>
    /// Initializes a new instance of <see cref="MyTokenService"/> and configures token settings (signing key, access/refresh validity)
    /// and the refresh-token endpoint.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <c>telemetryService</c>, <c>config</c>, or <c>jwtRepository</c> is null.</exception>
    public MyTokenService(IOptions<JwtConfig> config, IJwtRepository jwtRepository, ITelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(jwtRepository);

        _telemetryService = telemetryService;
        _jwtRepository = jwtRepository;
        
        Setup(o =>
        {
            o.TokenSigningKey = config.Value.PrivateKey;
            o.AccessTokenValidity = TimeSpan.FromMinutes(60);
            o.RefreshTokenValidity = TimeSpan.FromMinutes(50);

            o.Endpoint("/api/refresh-token", ep =>
            {
                ep.Summary(s => s.Summary = "this is the refresh token endpoint");
            });
        });
    }

    /// <summary>
    /// 이 메서드는 새로운 액세스/리프레시 토큰 쌍이 생성될 때마다 호출됩니다.
    /// 토큰과 만료일을 원하는 방식으로 저장하여 향후 리프레시 요청을 검증하는 데 사용하십시오.
    /// </summary>
    /// <param name="response">토큰 응답</param>
    /// <summary>
    /// Persists the given token response's refresh token for the user asynchronously and starts a telemetry activity for the operation.
    /// </summary>
    /// <param name="response">Token response containing the user ID and refresh token to store.</param>
    /// <returns>A task that completes when the refresh token has been persisted.</returns>
    public override async Task PersistTokenAsync(TokenResponse response)
    {
        using var span = _telemetryService.StartActivity("PersistTokenAsync");
        await _jwtRepository.StoreTokenAsync(response.UserId, response.RefreshToken);
    }

    /// <summary>
    /// 입력된 리프레시 요청을 검증하려면 토큰과 유효 기간을 이전에 저장된 데이터와 비교합니다.
    /// 토큰이 유효하지 않고 새로운 토큰 쌍을 생성하지 않아야 하는 경우,
    /// AddError() 메서드를 사용하여 검증 오류를 추가합니다.
    /// 추가한 오류는 요청한 클라이언트에게 전송됩니다. 오류가 추가되지 않으면
    /// 검증이 통과되며 새로운 토큰 쌍이 생성되어 클라이언트에게 전송됩니다.
    /// </summary>
    /// <param name="req">토큰 요청</param>
    /// <summary>
    /// Asynchronously validates the provided refresh token and records a validation error if it is invalid.
    /// </summary>
    /// <param name="req">The token renewal request containing the user identifier and refresh token to validate.</param>
    /// <returns>A task that completes when validation has finished.</returns>
    public override async Task RefreshRequestValidationAsync(TokenRequest req)
    {
        using var span = _telemetryService.StartActivity("RefreshRequestValidationAsync");
        if (await _jwtRepository.TokenIsValidAsync(req.UserId, req.RefreshToken) == false)
            AddError(r => r.RefreshToken, "Refresh token is invalid");
    }

    /// <summary>
    /// JWT에 포함될 사용자 권한을 지정합니다.
    /// 이 권한은 리프레시 요청이 수신되고 검증에 통과했을 때 적용됩니다.
    /// 이 설정은 리프레시 엔드포인트로 수신된 리뉴얼/리프레시 요청에만 적용되며,
    /// 초기 JWT 생성 시에는 적용되지 않습니다.
    /// </summary>
    /// <param name="request">토큰 요청</param>
    /// <param name="privileges">사용자 권한</param>
    /// <summary>
    /// Populates renewal privileges for a token renewal request by adding the Manager role, a UserId claim, and the Manager_Permission.
    /// </summary>
    /// <param name="request">The token renewal request; the user's identifier (Request.UserId) is added as a claim.</param>
    /// <param name="privileges">Mutable privileges object that will receive additional roles, claims, and permissions required for renewal.</param>
    /// <returns>A task that completes when the privileges have been set.</returns>
    public override async Task SetRenewalPrivilegesAsync(TokenRequest request, UserPrivileges privileges)
    {
        using var span = _telemetryService.StartActivity("SetRenewalPrivilegesAsync");
        privileges.Roles.Add("Manager");
        privileges.Claims.Add(new("UserId", request.UserId));
        privileges.Permissions.Add("Manager_Permission");
        await Task.CompletedTask;
    }
}