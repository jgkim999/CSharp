# SocketClient 사용 가이드

## 개요

`SocketClient`는 SuperSocket 서버와 통신하는 TCP 클라이언트입니다. 5바이트 고정 헤더(플래그 + 메시지 타입 + 길이) 프로토콜을 사용합니다.

## 패킷 구조

```
+--------+--------+--------+--------+--------+--------+--------+
| Flags  | Message Type    | Body Length     | Body Data      |
| 1 byte | 2 bytes         | 2 bytes         | N bytes        |
+--------+--------+--------+--------+--------+--------+--------+
```

### 플래그 비트
- 비트 0: 압축 여부
- 비트 1: 암호화 여부
- 비트 2-7: 예약됨

## 기본 사용법

### 1. 연결 및 기본 메시지 송수신

```csharp
using Demo.SimpleSocketClient.Services;
using Demo.SimpleSocket.SuperSocket;

// 클라이언트 생성
var client = new SocketClient();

// 이벤트 핸들러 등록
client.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"메시지 타입: {e.MessageType}");
    Console.WriteLine($"압축됨: {e.IsCompressed}");
    Console.WriteLine($"암호화됨: {e.IsEncrypted}");
    Console.WriteLine($"내용: {e.BodyText}");
};

client.Disconnected += (sender, e) =>
{
    Console.WriteLine("연결 해제됨");
};

client.ErrorOccurred += (sender, ex) =>
{
    Console.WriteLine($"에러 발생: {ex.Message}");
};

// 서버 연결
await client.ConnectAsync("localhost", 4040);

// 일반 메시지 전송 (플래그 없음)
await client.SendTextMessageAsync(SocketMessageType.Echo, "Hello, Server!");

// 연결 해제
client.Disconnect();
```

### 2. 플래그를 사용한 메시지 전송

```csharp
// 압축된 메시지 전송
var flags = PacketFlags.Compressed;
var body = Encoding.UTF8.GetBytes("압축된 메시지");
await client.SendMessageAsync(SocketMessageType.Echo, body, flags);

// 암호화된 메시지 전송
flags = PacketFlags.Encrypted;
body = Encoding.UTF8.GetBytes("암호화된 메시지");
await client.SendMessageAsync(SocketMessageType.Echo, body, flags);

// 압축 + 암호화된 메시지 전송
flags = PacketFlags.Compressed | PacketFlags.Encrypted;
body = Encoding.UTF8.GetBytes("압축 및 암호화된 메시지");
await client.SendMessageAsync(SocketMessageType.Echo, body, flags);

// 확장 메서드 사용
flags = PacketFlags.None
    .SetCompressed(true)
    .SetEncrypted(true);
await client.SendMessageAsync(SocketMessageType.Echo, body, flags);
```

### 3. MessagePack 객체 전송

```csharp
using MessagePack;

[MessagePackObject]
public class ChatMessage
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public string Message { get; set; }
}

// MessagePack 객체 전송
var message = new ChatMessage
{
    Name = "홍길동",
    Message = "안녕하세요"
};

await client.SendMessagePackAsync(SocketMessageType.MsgPack, message);
```

### 4. 메시지 수신 처리

```csharp
client.MessageReceived += (sender, e) =>
{
    // 플래그 확인
    if (e.IsCompressed)
    {
        Console.WriteLine("압축된 메시지 수신");
        // TODO: 압축 해제 로직
    }

    if (e.IsEncrypted)
    {
        Console.WriteLine("암호화된 메시지 수신");
        // TODO: 복호화 로직
    }

    // 메시지 타입별 처리
    switch ((SocketMessageType)e.MessageType)
    {
        case SocketMessageType.Echo:
            Console.WriteLine($"Echo: {e.BodyText}");
            break;

        case SocketMessageType.MsgPack:
            var chatMsg = e.DeserializeMessagePack<ChatMessage>();
            Console.WriteLine($"{chatMsg?.Name}: {chatMsg?.Message}");
            break;

        case SocketMessageType.Ping:
            // Pong 응답
            await client.SendTextMessageAsync(SocketMessageType.Pong, "pong");
            break;
    }
};
```

## API 레퍼런스

### 메서드

#### ConnectAsync
서버에 연결합니다.

```csharp
Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
```

#### Disconnect
서버 연결을 해제합니다.

```csharp
void Disconnect()
```

#### SendMessageAsync
바이너리 메시지를 전송합니다.

```csharp
// 플래그 없이 전송 (기본값: PacketFlags.None)
Task SendMessageAsync(SocketMessageType messageType, byte[] body, CancellationToken cancellationToken = default)
Task SendMessageAsync(ushort messageType, byte[] body, CancellationToken cancellationToken = default)

// 플래그 포함 전송
Task SendMessageAsync(SocketMessageType messageType, byte[] body, PacketFlags flags, CancellationToken cancellationToken = default)
Task SendMessageAsync(ushort messageType, byte[] body, PacketFlags flags, CancellationToken cancellationToken = default)
```

#### SendTextMessageAsync
UTF-8 텍스트 메시지를 전송합니다.

```csharp
Task SendTextMessageAsync(SocketMessageType messageType, string text, CancellationToken cancellationToken = default)
Task SendTextMessageAsync(ushort messageType, string text, CancellationToken cancellationToken = default)
```

#### SendMessagePackAsync
MessagePack 직렬화된 객체를 전송합니다.

```csharp
Task SendMessagePackAsync<T>(SocketMessageType messageType, T obj, CancellationToken cancellationToken = default)
Task SendMessagePackAsync<T>(ushort messageType, T obj, CancellationToken cancellationToken = default)
```

### 이벤트

#### MessageReceived
메시지 수신 시 발생합니다.

```csharp
event EventHandler<MessageReceivedEventArgs>? MessageReceived
```

**MessageReceivedEventArgs 속성:**
- `PacketFlags Flags`: 패킷 플래그
- `ushort MessageType`: 메시지 타입
- `byte[] Body`: 메시지 바디
- `string BodyText`: UTF-8 텍스트로 변환된 바디
- `bool IsCompressed`: 압축 여부
- `bool IsEncrypted`: 암호화 여부
- `T? DeserializeMessagePack<T>()`: MessagePack 역직렬화 메서드

#### Disconnected
연결 해제 시 발생합니다.

```csharp
event EventHandler? Disconnected
```

#### ErrorOccurred
에러 발생 시 발생합니다.

```csharp
event EventHandler<Exception>? ErrorOccurred
```

### 속성

#### IsConnected
현재 연결 상태를 반환합니다.

```csharp
bool IsConnected { get; }
```

## 고급 사용 예제

### 압축/암호화 처리

```csharp
using System.IO.Compression;

client.MessageReceived += async (sender, e) =>
{
    var body = e.Body;

    // 복호화
    if (e.IsEncrypted)
    {
        body = DecryptData(body);
    }

    // 압축 해제
    if (e.IsCompressed)
    {
        body = DecompressData(body);
    }

    var text = Encoding.UTF8.GetString(body);
    Console.WriteLine($"처리된 메시지: {text}");
};

byte[] CompressData(byte[] data)
{
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionMode.Compress))
    {
        gzip.Write(data);
    }
    return output.ToArray();
}

byte[] DecompressData(byte[] data)
{
    using var input = new MemoryStream(data);
    using var gzip = new GZipStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    gzip.CopyTo(output);
    return output.ToArray();
}

byte[] DecryptData(byte[] data)
{
    // TODO: 실제 복호화 로직 구현
    return data;
}

byte[] EncryptData(byte[] data)
{
    // TODO: 실제 암호화 로직 구현
    return data;
}
```

### 재연결 로직

```csharp
public async Task ConnectWithRetryAsync(string host, int port, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await client.ConnectAsync(host, port);
            Console.WriteLine("연결 성공");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"연결 실패 ({i + 1}/{maxRetries}): {ex.Message}");
            if (i < maxRetries - 1)
            {
                await Task.Delay(1000 * (i + 1)); // 지수 백오프
            }
        }
    }

    throw new Exception("최대 재시도 횟수 초과");
}
```

### Ping/Pong 구현

```csharp
var pingTimer = new Timer(async _ =>
{
    if (client.IsConnected)
    {
        await client.SendTextMessageAsync(SocketMessageType.Ping, "ping");
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

client.MessageReceived += async (sender, e) =>
{
    if ((SocketMessageType)e.MessageType == SocketMessageType.Pong)
    {
        Console.WriteLine("Pong 수신");
    }
};
```

## 주의사항

1. **Dispose 호출**: 사용 후 반드시 `Dispose()`를 호출하거나 `using` 구문을 사용하세요.

2. **스레드 안전성**: 여러 스레드에서 동시에 메시지를 전송할 경우 동기화가 필요할 수 있습니다.

3. **바디 크기 제한**: 바디 길이는 ushort(0-65535)로 제한됩니다.

4. **빅 엔디안**: 메시지 타입과 바디 길이는 빅 엔디안으로 인코딩됩니다.

5. **압축/암호화**: 플래그만 설정하고 실제 압축/암호화 처리를 하지 않으면 서버에서 올바르게 해석하지 못할 수 있습니다.