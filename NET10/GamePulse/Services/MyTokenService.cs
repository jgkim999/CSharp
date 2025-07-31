using FastEndpoints;
using FastEndpoints.Security;
using GamePulse.Configs;
using GamePulse.Repositories.Jwt;
using OpenTelemetry.Trace;

namespace GamePulse.Services;

/// <summary>
/// 
/// </summary>
public class MyTokenService : RefreshTokenService<TokenRequest, TokenResponse>
{
    private readonly ILogger<MyTokenService> _logger;
    private readonly IJwtRepository _jwtRepository;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <param name="jwtRepository"></param>
    /// <summary>
    /// Initializes a new instance of the <see cref="MyTokenService"/> class, configuring JWT token parameters and refresh token endpoint settings.
    /// </summary>
    /// <exception cref="NullReferenceException">
    /// Thrown if <paramref name="config"/>, <paramref name="logger"/>, <paramref name="jwtRepository"/>, or the "Jwt" configuration section is null.
    /// </exception>
    public MyTokenService(IConfiguration config, ILogger<MyTokenService> logger, IJwtRepository jwtRepository)
    {
        if (config is null || logger is null || jwtRepository is null)
            throw new NullReferenceException();
        _logger = logger;
        _jwtRepository = jwtRepository;
        
        var jwtConfig = config.GetSection("Jwt").Get<JwtConfig>();
        if (jwtConfig == null)
            throw new NullReferenceException();
        
        Setup(o =>
        {
            o.TokenSigningKey = jwtConfig.PrivateKey;
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
    /// <param name="response"></param>
    /// <returns></returns>
    /// <summary>
    /// Persists the generated access and refresh tokens along with their expiration details.
    /// </summary>
    /// <param name="response">The token response containing the tokens and their metadata to be stored.</param>
    public override async Task PersistTokenAsync(TokenResponse response)
    {
        using var span = GamePulseActivitySource.StartActivity("PersistTokenAsync");
        await _jwtRepository.StoreTokenAsync(response);
    }

    /// <summary>
    /// 입력된 리프레시 요청을 검증하려면 토큰과 유효 기간을 이전에 저장된 데이터와 비교합니다.
    /// 토큰이 유효하지 않고 새로운 토큰 쌍을 생성하지 않아야 하는 경우,
    /// AddError() 메서드를 사용하여 검증 오류를 추가합니다.
    /// 추가한 오류는 요청한 클라이언트에게 전송됩니다. 오류가 추가되지 않으면
    /// 검증이 통과되며 새로운 토큰 쌍이 생성되어 클라이언트에게 전송됩니다.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    /// <summary>
    /// Validates the refresh token in the incoming request and adds a validation error if the token is invalid.
    /// </summary>
    public override async Task RefreshRequestValidationAsync(TokenRequest req)
    {
        using var span = GamePulseActivitySource.StartActivity("RefreshRequestValidationAsync");
        if (await _jwtRepository.TokenIsValidAsync(req.UserId, req.RefreshToken) == false)
            AddError(r => r.RefreshToken, "Refresh token is invalid");
    }

    /// <summary>
    /// JWT에 포함될 사용자 권한을 지정합니다.
    /// 이 권한은 리프레시 요청이 수신되고 검증에 통과했을 때 적용됩니다.
    /// 이 설정은 리프레시 엔드포인트로 수신된 리뉴얼/리프레시 요청에만 적용되며,
    /// 초기 JWT 생성 시에는 적용되지 않습니다.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="privileges"></param>
    /// <returns></returns>
    /// <summary>
    /// Assigns renewal privileges to a user during token refresh, adding the "Manager" role, a "UserId" claim, and the "Manager_Permission" permission.
    /// </summary>
    /// <param name="request">The token request containing user information.</param>
    /// <param name="privileges">The user privileges object to be updated.</param>
    public override async Task SetRenewalPrivilegesAsync(TokenRequest request, UserPrivileges privileges)
    {
        using var span = GamePulseActivitySource.StartActivity("SetRenewalPrivilegesAsync");
        privileges.Roles.Add("Manager");
        privileges.Claims.Add(new("UserId", request.UserId));
        privileges.Permissions.Add("Manager_Permission");
        await Task.CompletedTask;
    }
}