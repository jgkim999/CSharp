# Demo.SimpleSocket

SuperSocket 기반의 고성능 TCP 소켓 서버입니다. 메시지 압축, AES 암호화, MessagePack 직렬화를 지원하며, ASP.NET Core와 통합되어 있습니다.

## 주요 기능

- **SuperSocket 통합**: 고성능 비동기 TCP 서버
- **메시지 압축**: GZip 압축으로 네트워크 대역폭 절약 (512바이트 이상 자동 압축)
- **AES-256 암호화**: 세션별 독립적인 Key/IV를 사용한 종단간 암호화
- **MessagePack**: 바이너리 직렬화로 효율적인 데이터 전송
- **OpenTelemetry**: 분산 추적 및 메트릭 수집
- **Serilog**: 구조화된 로깅
- **FastEndpoints**: REST API 엔드포인트
- **멀티 클라이언트**: 동시 다중 클라이언트 연결 지원

## 프로토콜 구조

### 패킷 헤더 (8바이트 고정)

```
+-------+----------+----------+--------------+-------------+
| Flags | Sequence | Reserved | Message Type | Body Length |
+-------+----------+----------+--------------+-------------+
| 1byte | 2bytes   | 1byte    | 2bytes       | 2bytes      |
+-------+----------+----------+--------------+-------------+
```

- **Flags (1바이트)**: 압축/암호화 플래그
  - Bit 0: 압축 여부 (1 = GZip 압축됨)
  - Bit 1: 암호화 여부 (1 = AES 암호화됨)
- **Sequence (2바이트)**: 메시지 순서 번호 (Big Endian)
- **Reserved (1바이트)**: 예약 필드
- **Message Type (2바이트)**: 메시지 타입 (Big Endian)
- **Body Length (2바이트)**: 바디 길이 (Big Endian, 최대 65535바이트)

### 메시지 처리 순서

#### 송신 (클라이언트 → 서버)
1. MessagePack 직렬화
2. 압축 (512바이트 이상이면 GZip 압축)
3. 암호화 (요청 시 AES-256 암호화)
4. 헤더 추가 및 전송

#### 수신 (서버 → 클라이언트)
1. 헤더 파싱
2. 복호화 (암호화 플래그가 있으면)
3. 압축 해제 (압축 플래그가 있으면)
4. MessagePack 역직렬화

## 메시지 타입

### 서버 → 클라이언트

| Type | 이름 | 설명 |
|------|------|------|
| 1    | ConnectionSuccess | 연결 성공 (AES Key/IV 포함) |
| 2    | Ping | 서버 → 클라이언트 핑 |
| 6    | MsgPackResponse | MessagePack 응답 |
| 8    | VeryLongRes | 긴 텍스트 응답 (압축 테스트용) |

### 클라이언트 → 서버

| Type | 이름 | 설명 |
|------|------|------|
| 3    | Pong | 클라이언트 → 서버 퐁 |
| 5    | MsgPackRequest | MessagePack 요청 |
| 7    | VeryLongReq | 긴 텍스트 요청 (압축 테스트용) |

## 설정

### appsettings.json

```json
{
  "ServerOptions": {
    "Name": "SimpleSocket",
    "Listeners": [
      {
        "Ip": "0.0.0.0",
        "Port": 4040
      }
    ]
  },
  "CustomServerOption": {
    "PingIntervalSeconds": 30
  }
}
```

### 주요 설정 옵션

- **ServerOptions.Listeners**: 리스닝 IP와 포트
- **CustomServerOption.PingIntervalSeconds**: Ping 전송 간격 (초)

## 실행 방법

### 1. 프로젝트 빌드

```bash
dotnet build Demo.SimpleSocket/Demo.SimpleSocket.csproj
```

### 2. 서버 실행

```bash
cd Demo.SimpleSocket
dotnet run
```

또는 특정 환경으로 실행:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

### 3. 서버 확인

서버가 시작되면 다음 주소에서 API 문서를 확인할 수 있습니다:

- OpenAPI: `http://localhost:5000/openapi/v1.json`
- Scalar API Reference: `http://localhost:5000/scalar/v1`

## 주요 클래스

### DemoSession

클라이언트 세션을 관리하는 클래스입니다.

```csharp
public class DemoSession : AppSession
{
    // 세션별 AES Key/IV
    public byte[] AesKey { get; set; }
    public byte[] AesIV { get; set; }

    // 압축/암호화 처리
    public ISessionCompression? Compression { get; set; }
    public ISessionEncryption? Encryption { get; set; }
}
```

### IClientSocketMessageHandler

메시지 처리를 담당하는 핸들러입니다.

```csharp
public interface IClientSocketMessageHandler
{
    Task HandleMessageAsync(IAppSession session, BinaryPackageInfo package);
}
```

### ISessionManager

세션 라이프사이클을 관리합니다.

```csharp
public interface ISessionManager
{
    Task OnConnectAsync(IAppSession session);
    Task OnDisconnectAsync(IAppSession session, CloseReason reason);
    Task OnMessageAsync(IAppSession session, BinaryPackageInfo package);
}
```

## 암호화

### AES-256 암호화

각 클라이언트 세션마다 독립적인 AES Key와 IV가 생성되며, 연결 성공 시 클라이언트로 전송됩니다.

```csharp
// 연결 성공 메시지에 AES Key/IV 포함
var response = new MsgConnectionSuccessNfy
{
    ConnectionId = session.SessionID,
    ServerUtcTime = DateTime.UtcNow,
    AesKey = demoSession.AesKey,
    AesIV = demoSession.AesIV
};
```

클라이언트는 이 Key/IV를 사용하여 이후 메시지를 암호화/복호화합니다.

## 압축

### GZip 압축

512바이트 이상의 메시지는 자동으로 GZip 압축됩니다.

```csharp
// 자동 압축 기준
public const int AutoCompressThreshold = 512;

// 압축 후 플래그 설정
flags = flags.SetCompressed(true);
```

## 모니터링

### OpenTelemetry

메트릭, 트레이스, 로그를 수집하여 관찰성을 제공합니다.

- **메트릭**: 연결 수, 메시지 처리 시간 등
- **트레이스**: 분산 추적
- **로그**: Serilog 통합

### Serilog

구조화된 로깅을 통해 운영 중 문제를 추적할 수 있습니다.

```bash
# 로그 확인
tail -f logs/simple-socket-*.log
```

## 성능 최적화

### ArrayPool 사용

메모리 할당을 최소화하기 위해 `ArrayPool<byte>.Shared`를 사용합니다.

```csharp
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // 버퍼 사용
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### RecyclableMemoryStream

압축/압축 해제 시 `RecyclableMemoryStreamManager`를 사용하여 메모리 할당을 줄입니다.

## 개발 가이드

### 새로운 메시지 타입 추가

1. `Demo.SimpleSocketShare/SocketMessageType.cs`에 타입 정의
2. `Demo.SimpleSocketShare/Messages/`에 메시지 DTO 추가
3. `ClientSocketMessageHandler.cs`에 핸들러 구현

### 로깅

```csharp
_logger.LogInformation("Client connected: {SessionId}", session.SessionID);
_logger.LogError(exception, "Error processing message: {MessageType}", messageType);
```

## 문제 해결

### 연결 문제

1. 방화벽 확인: 포트 4040이 열려있는지 확인
2. 로그 확인: `logs/simple-socket-*.log` 파일 확인
3. 네트워크 연결 확인: `telnet localhost 4040`

### 성능 문제

1. OpenTelemetry 메트릭 확인
2. 로그에서 느린 처리 시간 확인
3. 압축/암호화 오버헤드 분석

## 라이선스

이 프로젝트는 교육 목적으로 작성되었습니다.