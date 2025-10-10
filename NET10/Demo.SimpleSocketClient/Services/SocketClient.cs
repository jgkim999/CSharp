using System;

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Demo.Application.Utils;
using Demo.SimpleSocketShare;
using MessagePack;
using Microsoft.IO;

namespace Demo.SimpleSocketClient.Services;

/// <summary>
/// SimpleSocket 서버와 통신하는 TCP 클라이언트
/// 프로토콜: 8바이트 헤더(플래그 1 + 시퀀스 2 + 예약 1 + 메시지타입 2 + 길이 2) + 바디
/// </summary>
public class SocketClient : IDisposable
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly Faker _faker = new("ko");
    private readonly SequenceGenerator _sendSequence = new();
    private CancellationTokenSource? _receiveCts;
    private bool _disposed;
    private byte[] _aesKey = Array.Empty<byte>();
    private byte[] _aesIV = Array.Empty<byte>();
    private Aes? _aes;  // Aes 인스턴스 재사용

    /// <summary>
    /// 연결 상태
    /// </summary>
    public bool IsConnected => _tcpClient?.Connected ?? false;

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// 연결 해제 이벤트
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// 에러 발생 이벤트
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// 로그 메시지 핸들러 (Console 출력 대신 사용)
    /// </summary>
    public Action<string>? LogHandler { get; set; }

    /// <summary>
    /// 서버에 연결
    /// </summary>
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            throw new InvalidOperationException("이미 연결되어 있습니다.");

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port, cancellationToken);
            _stream = _tcpClient.GetStream();

            // 수신 시작
            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);
        }
        catch
        {
            _tcpClient?.Dispose();
            _tcpClient = null;
            _stream = null;
            throw;
        }
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    public void Disconnect()
    {
        if (!IsConnected)
            return;

        try
        {
            _receiveCts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
        finally
        {
            _receiveCts?.Dispose();
            _receiveCts = null;
            _stream = null;
            _tcpClient = null;

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
    

    /// <summary>
    /// GZip을 사용하여 데이터 압축
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    /// <returns>(압축된 버퍼, 실제 압축 데이터 길이)</returns>
    private (byte[] Buffer, int Length) CompressData(ReadOnlySpan<byte> data)
    {
        // tag 제거: 다중 인스턴스 환경에서 안정성 향상
        using var output = _memoryStreamManager.GetStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data);
        }

        // RecyclableMemoryStream에서 버퍼를 가져와 ArrayPool 버퍼로 복사
        var compressedLength = (int)output.Length;
        var result = _arrayPool.Rent(compressedLength);
        output.Position = 0;
        output.ReadExactly(result.AsSpan(0, compressedLength));
        return (result, compressedLength);
    }

    /// <summary>
    /// GZip으로 압축된 데이터 압축 해제
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    private byte[] DecompressData(ReadOnlySpan<byte> compressedData)
    {
        var compressedSize = compressedData.Length;

        // tag 제거: 다중 인스턴스 환경에서 안정성 향상
        using var input = _memoryStreamManager.GetStream();
        input.Write(compressedData);
        input.Position = 0;

        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = _memoryStreamManager.GetStream();

        gzip.CopyTo(output);
        var decompressed = output.ToArray();

        LogHandler?.Invoke($"[서버 압축 해제] 압축: {compressedSize} 바이트 → 원본: {decompressed.Length} 바이트 ({compressedSize * 100.0 / decompressed.Length:F1}%)");

        return decompressed;
    }

    /// <summary>
    /// MessagePack 객체 전송 (enum 버전)
    /// </summary>
    public Task SendMessagePackAsync<T>(SocketMessageType messageType, T obj, CancellationToken cancellationToken = default)
    {
        return SendMessagePackAsync((ushort)messageType, obj, cancellationToken);
    }

    /// <summary>
    /// MessagePack 객체 전송 (ushort 버전)
    /// ArrayBufferWriter를 사용하여 메모리 할당 최소화, ToArray() 제거로 추가 최적화
    /// </summary>
    private Task SendMessagePackAsync<T>(ushort messageType, T obj, CancellationToken cancellationToken = default)
    {
        // ArrayBufferWriter를 사용하면 내부적으로 ArrayPool을 활용하여 효율적
        var bufferWriter = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(bufferWriter, obj);
        // WrittenMemory를 직접 사용하여 ToArray() 호출 제거 (zero-copy)
        return SendMessageAsync(messageType, bufferWriter.WrittenMemory, PacketFlags.None, cancellationToken);
    }
    
    /// <summary>
    /// 수신 루프
    /// ArrayPool을 사용하여 메모리 할당 최소화
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        // 헤더 버퍼는 수신 루프 전체에서 재사용 (8바이트 고정)
        var headerBuffer = new byte[SocketConst.HeadSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected && _stream != null)
            {
                // 헤더 수신 (8바이트) - 완전히 읽을 때까지 반복
                var headerBytesRead = 0;
                while (headerBytesRead < SocketConst.HeadSize)
                {
                    var read = await _stream.ReadAsync(
                        headerBuffer.AsMemory(headerBytesRead, SocketConst.HeadSize - headerBytesRead),
                        cancellationToken);

                    if (read == 0)
                    {
                        // 서버가 연결을 끊음
                        Disconnect();
                        return;
                    }

                    headerBytesRead += read;
                }

                // 헤더 파싱
                var flags = (PacketFlags)headerBuffer[SocketConst.FlagStart];
                var sequence = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(SocketConst.SequenceStart, SocketConst.SequenceSize));
                var reserved = headerBuffer[SocketConst.ReservedStart];
                var messageType = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(SocketConst.MessageTypeStart, SocketConst.MessageTypeSize));
                var bodyLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(SocketConst.BodySizeStart, SocketConst.BodySize));

                // 바디 수신 - ArrayPool 사용으로 GC 압력 감소
                byte[] body;

                if (bodyLength > 0)
                {
                    // ArrayPool에서 버퍼 빌리기
                    var rentedBuffer = _arrayPool.Rent(bodyLength);
                    var bodyBytesRead = 0;

                    try
                    {
                        while (bodyBytesRead < bodyLength)
                        {
                            var read = await _stream.ReadAsync(
                                rentedBuffer.AsMemory(bodyBytesRead, bodyLength - bodyBytesRead),
                                cancellationToken);

                            if (read == 0)
                            {
                                Disconnect();
                                return;
                            }

                            bodyBytesRead += read;
                        }

                        // 수신 데이터 처리: 암호화 → 압축 → 원본 순서로 복원
                        byte[]? decryptedBuffer = null;
                        int decryptedLength = 0;
                        ReadOnlySpan<byte> processedData = rentedBuffer.AsSpan(0, bodyLength);

                        try
                        {
                            // 1단계: 암호화된 데이터인 경우 먼저 복호화 (ArrayPool 사용)
                            if (flags.IsEncrypted())
                            {
                                (decryptedBuffer, decryptedLength) = DecryptDataToPool(processedData);
                                processedData = decryptedBuffer.AsSpan(0, decryptedLength);
                            }

                            // 2단계: 압축된 데이터인 경우 압축 해제
                            if (flags.IsCompressed())
                            {
                                body = DecompressData(processedData);
                            }
                            else
                            {
                                // 실제 사용한 크기만큼만 복사
                                body = new byte[processedData.Length];
                                processedData.CopyTo(body);
                            }
                        }
                        finally
                        {
                            // ArrayPool 버퍼 반환
                            if (decryptedBuffer != null)
                            {
                                _arrayPool.Return(decryptedBuffer);
                            }
                        }
                    }
                    finally
                    {
                        // ArrayPool에 버퍼 반납
                        _arrayPool.Return(rentedBuffer);
                    }
                }
                else
                {
                    body = Array.Empty<byte>();
                }

                // 메시지 수신 이벤트 발생
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(flags, sequence, reserved, messageType, body));
            }
        }
        catch (OperationCanceledException)
        {
            // 정상 종료
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            Disconnect();
        }
    }

    /// <summary>
    /// AES Key/IV 설정 및 Aes 인스턴스 초기화
    /// </summary>
    public void SetAesKey(byte[] key, byte[] iv)
    {
        _aesKey = key;
        _aesIV = iv;

        // 기존 Aes 인스턴스 해제
        _aes?.Dispose();

        // 새로운 Aes 인스턴스 생성 및 설정
        _aes = Aes.Create();
        _aes.Key = key;
        _aes.IV = iv;
        _aes.Mode = CipherMode.CBC;
        _aes.Padding = PaddingMode.PKCS7;

        LogHandler?.Invoke($"[클라이언트 AES 초기화] KeySize: {key.Length}, IVSize: {iv.Length}");
    }

    /// <summary>
    /// MessagePack 객체 전송 (암호화 옵션 포함)
    /// </summary>
    public Task SendMessagePackAsync<T>(SocketMessageType messageType, T obj, bool encrypt, CancellationToken cancellationToken = default)
    {
        return SendMessagePackAsync((ushort)messageType, obj, encrypt, cancellationToken);
    }

    /// <summary>
    /// MessagePack 객체 전송 (ushort 버전, 암호화 옵션 포함)
    /// </summary>
    private async Task SendMessagePackAsync<T>(ushort messageType, T obj, bool encrypt, CancellationToken cancellationToken = default)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        MessagePackSerializer.Serialize(bufferWriter, obj);
        ReadOnlyMemory<byte> bodyMemory = bufferWriter.WrittenMemory;

        byte[]? compressedBuffer = null;
        byte[]? encryptedBuffer = null;
        int encryptedLength = 0;
        PacketFlags flags = PacketFlags.None;

        try
        {
            // 1단계: 압축 (512바이트 이상이면 자동 압축)
            if (bodyMemory.Length > SocketConst.AutoCompressThreshold)
            {
                var originalSize = bodyMemory.Length;
                int compressedLength;
                (compressedBuffer, compressedLength) = CompressData(bodyMemory.Span);
                bodyMemory = compressedBuffer.AsMemory(0, compressedLength);
                flags = flags.SetCompressed(true);  // 압축 플래그 설정

                LogHandler?.Invoke($"[클라이언트 압축] 원본: {originalSize} 바이트 → 압축: {compressedLength} 바이트 ({compressedLength * 100.0 / originalSize:F1}%)");
            }

            // 2단계: 암호화 (encrypt=true이면 암호화) - Aes 인스턴스 재사용
            if (encrypt)
            {
                if (_aes == null)
                {
                    throw new InvalidOperationException("AES가 초기화되지 않았습니다. SetAesKey()를 먼저 호출하세요.");
                }

                var originalSize = bodyMemory.Length;
                (encryptedBuffer, encryptedLength) = EncryptDataToPool(bodyMemory.Span);
                bodyMemory = encryptedBuffer.AsMemory(0, encryptedLength);
                flags = flags.SetEncrypted(true);
                LogHandler?.Invoke($"[클라이언트 암호화] 원본: {originalSize} 바이트 → 암호화: {encryptedLength} 바이트");
            }

            await SendMessageAsync(messageType, bodyMemory, flags, cancellationToken);
        }
        finally
        {
            if (compressedBuffer != null)
            {
                _arrayPool.Return(compressedBuffer);
            }
            // 암호화 버퍼 ArrayPool 반환
            if (encryptedBuffer != null)
            {
                _arrayPool.Return(encryptedBuffer);
            }
        }
    }

    /// <summary>
    /// 메시지 전송 (플래그 + 시퀀스 포함, ushort 버전)
    /// ReadOnlyMemory를 사용하여 불필요한 복사 제거 (async 호환)
    /// </summary>
    private async Task SendMessageAsync(ushort messageType, ReadOnlyMemory<byte> body, PacketFlags flags, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다.");

        ReadOnlyMemory<byte> processedBody = body;
        var bodyLength = (ushort)processedBody.Length;
        var packetLength = SocketConst.HeadSize + bodyLength;

        // ArrayPool에서 버퍼 빌리기 (GC 압력 감소)
        var packet = _arrayPool.Rent(packetLength);
        try
        {
            // 헤더 작성 (8바이트)
            packet[SocketConst.FlagStart] = (byte)flags;  // 플래그 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(SocketConst.SequenceStart, SocketConst.SequenceSize), _sendSequence.GetNext());  // 시퀀스 (2바이트)
            packet[SocketConst.ReservedStart] = _faker.Random.Byte(1, 255);  // 예약 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(SocketConst.MessageTypeStart, SocketConst.MessageTypeSize), messageType);  // 메시지 타입 (2바이트)
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(SocketConst.BodySizeStart, SocketConst.BodySize), bodyLength);   // 바디 길이 (2바이트)

            // 바디 복사 (ReadOnlyMemory -> Span 직접 복사)
            if (bodyLength > 0)
                processedBody.Span.CopyTo(packet.AsSpan(SocketConst.HeadSize));

            // 실제 사용하는 크기만큼만 전송
            await _stream.WriteAsync(packet.AsMemory(0, packetLength), cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            // ArrayPool에 버퍼 반납
            _arrayPool.Return(packet);
        }
    }
    
    /// <summary>
    /// 데이터 암호화 (ArrayPool 사용, Aes 인스턴스 재사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(암호화된 버퍼, 실제 데이터 길이)</returns>
    private (byte[] Buffer, int Length) EncryptDataToPool(ReadOnlySpan<byte> data)
    {
        if (_aes == null)
        {
            throw new InvalidOperationException("AES가 초기화되지 않았습니다.");
        }

        using var encryptor = _aes.CreateEncryptor();

        var inputBuffer = _arrayPool.Rent(data.Length);
        try
        {
            data.CopyTo(inputBuffer);
            var encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, data.Length);

            // 암호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = _arrayPool.Rent(encrypted.Length);
            encrypted.CopyTo(outputBuffer, 0);

            return (outputBuffer, encrypted.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }

    /// <summary>
    /// 데이터 복호화 (ArrayPool 사용, Aes 인스턴스 재사용)
    /// 반환된 버퍼는 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(복호화된 버퍼, 실제 데이터 길이)</returns>
    private (byte[] Buffer, int Length) DecryptDataToPool(ReadOnlySpan<byte> encryptedData)
    {
        if (_aes == null)
        {
            throw new InvalidOperationException("AES가 초기화되지 않았습니다.");
        }

        using var decryptor = _aes.CreateDecryptor();

        var inputBuffer = _arrayPool.Rent(encryptedData.Length);
        try
        {
            encryptedData.CopyTo(inputBuffer);
            var decrypted = decryptor.TransformFinalBlock(inputBuffer, 0, encryptedData.Length);

            // 복호화된 데이터를 ArrayPool 버퍼로 복사
            var outputBuffer = _arrayPool.Rent(decrypted.Length);
            decrypted.CopyTo(outputBuffer, 0);

            LogHandler?.Invoke($"[클라이언트 복호화] 암호화: {encryptedData.Length} 바이트 → 원본: {decrypted.Length} 바이트");

            return (outputBuffer, decrypted.Length);
        }
        finally
        {
            _arrayPool.Return(inputBuffer);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Disconnect();

        // Aes 인스턴스 해제
        _aes?.Dispose();
        _aes = null;

        _disposed = true;
    }
}
