# FixedHeaderPipelineFilter 사용 가이드

## 패킷 구조

패킷은 총 5바이트의 고정 헤더와 가변 길이 바디로 구성됩니다:

```
+--------+--------+--------+--------+--------+--------+--------+
| Flags  | Message Type    | Body Length     | Body Data      |
| 1 byte | 2 bytes         | 2 bytes         | N bytes        |
+--------+--------+--------+--------+--------+--------+--------+
```

### 헤더 구조 상세

1. **Flags (1바이트)**
   - 비트 0 (LSB): 압축 여부 (0=압축 안함, 1=압축됨)
   - 비트 1: 암호화 여부 (0=암호화 안함, 1=암호화됨)
   - 비트 2-7: 예약됨 (미래 사용을 위해 0으로 설정)

2. **Message Type (2바이트, BigEndian)**
   - 메시지 타입을 나타내는 ushort 값

3. **Body Length (2바이트, BigEndian)**
   - 바디 데이터의 길이 (최대 65535바이트)

4. **Body Data (가변 길이)**
   - 실제 메시지 데이터

## 사용 예제

### 1. 패킷 플래그 사용

```csharp
using Demo.SimpleSocket.SuperSocket;

// 플래그 생성
var flags = PacketFlags.None;
flags = flags.SetCompressed(true);  // 압축 플래그 설정
flags = flags.SetEncrypted(true);   // 암호화 플래그 설정

// 플래그 확인
bool isCompressed = flags.IsCompressed();  // true
bool isEncrypted = flags.IsEncrypted();    // true

// 비트 연산으로 직접 설정
var flags2 = PacketFlags.Compressed | PacketFlags.Encrypted;
```

### 2. 패킷 송신

```csharp
using System.Buffers.Binary;

// 패킷 데이터 준비
var flags = PacketFlags.Compressed | PacketFlags.Encrypted;
ushort messageType = 100;
byte[] bodyData = "Hello, World!"u8.ToArray();
ushort bodyLength = (ushort)bodyData.Length;

// 5바이트 헤더 + 바디 크기만큼 버퍼 할당
byte[] packet = new byte[5 + bodyLength];

// 헤더 작성
packet[0] = (byte)flags;
BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1, 2), messageType);
BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(3, 2), bodyLength);

// 바디 복사
bodyData.CopyTo(packet.AsSpan(5));

// 소켓으로 전송
// await socket.SendAsync(packet);
```

### 3. 패킷 수신 및 해석

```csharp
// SuperSocket이 자동으로 FixedHeaderPipelineFilter를 통해 패킷을 파싱합니다
public class MySocketHandler : IAsyncSessionHandler
{
    public async ValueTask HandleSessionAsync(IAppSession session)
    {
        await foreach (var package in session.Channel.RunAsync<BinaryPackageInfo>())
        {
            // 플래그 확인
            if (package.Flags.IsCompressed())
            {
                // 압축 해제 로직
                Console.WriteLine("압축된 패킷 수신");
            }

            if (package.Flags.IsEncrypted())
            {
                // 복호화 로직
                Console.WriteLine("암호화된 패킷 수신");
            }

            // 메시지 타입별 처리
            switch (package.MessageType)
            {
                case 100:
                    // 바디 데이터 처리
                    var bodySpan = package.BodySpan;
                    var message = Encoding.UTF8.GetString(bodySpan);
                    Console.WriteLine($"메시지: {message}");
                    break;
            }

            // 패킷 사용 후 반드시 Dispose 호출 (ArrayPool 반환)
            package.Dispose();
        }
    }
}
```

### 4. 압축/암호화 처리 예제

```csharp
using System.IO.Compression;

public static class PacketHelper
{
    // 패킷 생성 (압축 + 암호화 옵션)
    public static byte[] CreatePacket(
        ushort messageType,
        byte[] bodyData,
        bool compress = false,
        bool encrypt = false)
    {
        var flags = PacketFlags.None;
        var processedBody = bodyData;

        // 압축
        if (compress)
        {
            flags = flags.SetCompressed(true);
            processedBody = CompressData(processedBody);
        }

        // 암호화
        if (encrypt)
        {
            flags = flags.SetEncrypted(true);
            processedBody = EncryptData(processedBody);
        }

        ushort bodyLength = (ushort)processedBody.Length;
        byte[] packet = new byte[5 + bodyLength];

        // 헤더 작성
        packet[0] = (byte)flags;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1, 2), messageType);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(3, 2), bodyLength);
        processedBody.CopyTo(packet.AsSpan(5));

        return packet;
    }

    // 패킷 처리
    public static byte[] ProcessReceivedPacket(BinaryPackageInfo package)
    {
        var bodyData = package.BodySpan.ToArray();

        // 복호화
        if (package.Flags.IsEncrypted())
        {
            bodyData = DecryptData(bodyData);
        }

        // 압축 해제
        if (package.Flags.IsCompressed())
        {
            bodyData = DecompressData(bodyData);
        }

        return bodyData;
    }

    private static byte[] CompressData(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(data);
        }
        return output.ToArray();
    }

    private static byte[] DecompressData(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] EncryptData(byte[] data)
    {
        // TODO: 실제 암호화 로직 구현
        return data;
    }

    private static byte[] DecryptData(byte[] data)
    {
        // TODO: 실제 복호화 로직 구현
        return data;
    }
}
```

## 주의사항

1. **ArrayPool 사용**: `BinaryPackageInfo`는 내부적으로 `ArrayPool`을 사용하므로 반드시 `Dispose()`를 호출해야 합니다.

2. **바디 길이 제한**: 바디 길이는 ushort(0-65535)로 제한됩니다. 더 큰 데이터가 필요한 경우 프로토콜 수정이 필요합니다.

3. **빅 엔디안**: 메시지 타입과 바디 길이는 빅 엔디안으로 인코딩됩니다.

4. **플래그 예약 비트**: 현재 비트 0-1만 사용하며, 비트 2-7은 미래를 위해 예약되어 있습니다.

5. **압축/암호화 순서**: 압축을 먼저 한 후 암호화를 적용하는 것이 일반적으로 더 효율적입니다.