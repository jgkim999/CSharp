# CleanArchitecture 적용을 위한 Config 클래스 이동 작업

## 작업 개요
CleanArchitecture 원칙에 따라 GamePulse 프로젝트의 설정 클래스들을 Demo.Web 프로젝트로 이동하는 작업을 수행했습니다.

## 이동된 파일들
1. `GamePulse/Configs/JwtConfig.cs` → `Demo.Web/Configs/JwtConfig.cs`
2. `GamePulse/Configs/OtelConfig.cs` → `Demo.Web/Configs/OtelConfig.cs`

## 수행된 작업

### 1. 파일 이동 및 네임스페이스 수정
- JwtConfig.cs와 OtelConfig.cs를 Demo.Web/Configs 폴더로 복사
- 네임스페이스를 `GamePulse.Configs`에서 `Demo.Web.Configs`로 변경
- XML 주석 추가로 코드 문서화 개선

### 2. 참조 업데이트
다음 파일들의 using 문을 수정했습니다:
- `GamePulse/Program.cs`
- `GamePulse/OpenTelemetryInitialize.cs`
- `GamePulse/Services/MyTokenService.cs`
- `GamePulse/Sod/SodInitialize.cs`

### 3. 프로젝트 참조 추가
- `GamePulse/GamePulse.csproj`에 Demo.Web 프로젝트 참조 추가

### 4. 원본 파일 삭제
- GamePulse 프로젝트에서 원본 Config 파일들 삭제

## 변경 사항 상세

### JwtConfig.cs
```csharp
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
```

### OtelConfig.cs
```csharp
namespace Demo.Web.Configs;

/// <summary>
/// OpenTelemetry 설정
/// </summary>
public class OtelConfig
{
    /// <summary>
    /// OpenTelemetry 엔드포인트
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 트레이스 샘플러 인수
    /// </summary>
    public string TracesSamplerArg { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 이름
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 버전
    /// </summary>
    public string ServiceVersion { get; set; } = string.Empty;
}
```

## CleanArchitecture 관점에서의 개선점

1. **관심사의 분리**: 설정 클래스들이 웹 계층(Demo.Web)으로 이동하여 각 계층의 책임이 명확해졌습니다.

2. **의존성 방향**: GamePulse가 Demo.Web을 참조하도록 하여 설정 관리의 중앙화를 달성했습니다.

3. **코드 문서화**: XML 주석을 추가하여 각 속성의 역할과 목적을 명확히 했습니다.

## 검증 사항
- [ ] 프로젝트 빌드 성공 확인
- [ ] JWT 인증 기능 정상 동작 확인
- [ ] OpenTelemetry 메트릭 수집 정상 동작 확인
- [ ] 모든 참조가 올바르게 업데이트되었는지 확인

## 다음 단계
이 작업을 통해 설정 관리가 중앙화되었으며, 향후 다른 프로젝트에서도 동일한 설정을 재사용할 수 있는 기반이 마련되었습니다.