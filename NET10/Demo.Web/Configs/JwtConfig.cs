namespace Demo.Web.Configs;

/// <summary>
/// Jwt 인증
/// </summary>
public class JwtConfig
{
    /// <summary>
    /// JWT를 대칭적으로 서명하는 데 사용되는 키이거나 jwts가 비대칭적으로 서명될 때 base64로 인코딩된 공개 키입니다.
    /// 공개 키 검색이 동적으로 발생하는 IDP에서 발급한 토큰을 확인하는 데 사용되는 경우 키는 선택 사항일 수 있습니다.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT를 대칭적으로 서명하는 데 사용되는 키 또는 JWT가 비대칭적으로 서명될 때 사용되는 Base64로 인코딩된 개인 키.
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;
}