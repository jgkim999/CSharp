using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Demo.Application.DTO.Socket;
using Demo.Application.Utils;
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
    /// 메시지 전송 (플래그 + 시퀀스 포함, ushort 버전)
    /// ReadOnlyMemory를 사용하여 불필요한 복사 제거 (async 호환)
    /// 512바이트 이상의 바디는 자동으로 GZip 압축
    /// </summary>
    private async Task SendMessageAsync(ushort messageType, ReadOnlyMemory<byte> body, PacketFlags flags, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다.");

        // 512바이트 이상이면 압축 수행
        ReadOnlyMemory<byte> processedBody = body;
        byte[]? compressedBuffer = null;

        if (body.Length > 512)
        {
            compressedBuffer = CompressData(body.Span);
            processedBody = compressedBuffer;
            flags |= PacketFlags.Compressed;  // 압축 플래그 설정
        }

        try
        {
            var bodyLength = (ushort)processedBody.Length;
            var packetLength = 8 + bodyLength;

            // ArrayPool에서 버퍼 빌리기 (GC 압력 감소)
            var packet = _arrayPool.Rent(packetLength);

            try
            {
                // 헤더 작성 (8바이트)
                packet[0] = (byte)flags;  // 플래그 (1바이트)
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1, 2), _sendSequence.GetNext());  // 시퀀스 (2바이트)
                packet[3] = _faker.Random.Byte(1, 255);  // 예약 (1바이트)
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), messageType);  // 메시지 타입 (2바이트)
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6, 2), bodyLength);   // 바디 길이 (2바이트)

                // 바디 복사 (ReadOnlyMemory -> Span 직접 복사)
                if (bodyLength > 0)
                    processedBody.Span.CopyTo(packet.AsSpan(8));

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
        finally
        {
            // 압축 버퍼 해제
            if (compressedBuffer != null)
            {
                _arrayPool.Return(compressedBuffer);
            }
        }
    }

    /// <summary>
    /// GZip을 사용하여 데이터 압축
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    private byte[] CompressData(ReadOnlySpan<byte> data)
    {
        using var output = _memoryStreamManager.GetStream("SocketClient-Compress");
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data);
        }

        // RecyclableMemoryStream에서 버퍼를 가져와 ArrayPool 버퍼로 복사
        var compressedLength = (int)output.Length;
        var result = _arrayPool.Rent(compressedLength);
        output.Position = 0;
        output.ReadExactly(result.AsSpan(0, compressedLength));
        return result;
    }

    /// <summary>
    /// GZip으로 압축된 데이터 압축 해제
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    private byte[] DecompressData(ReadOnlySpan<byte> compressedData)
    {
        using var input = _memoryStreamManager.GetStream("SocketClient-Decompress-Input", compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = _memoryStreamManager.GetStream("SocketClient-Decompress-Output");

        gzip.CopyTo(output);
        return output.ToArray();
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
        var headerBuffer = new byte[8];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected && _stream != null)
            {
                // 헤더 수신 (8바이트) - 완전히 읽을 때까지 반복
                var headerBytesRead = 0;
                while (headerBytesRead < 8)
                {
                    var read = await _stream.ReadAsync(
                        headerBuffer.AsMemory(headerBytesRead, 8 - headerBytesRead),
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
                var flags = (PacketFlags)headerBuffer[0];
                var sequence = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(1, 2));
                var reserved = headerBuffer[3];
                var messageType = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(4, 2));
                var bodyLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(6, 2));

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

                        // 압축 플래그가 설정되어 있으면 압축 해제
                        if (flags.HasFlag(PacketFlags.Compressed))
                        {
                            body = DecompressData(rentedBuffer.AsSpan(0, bodyLength));
                        }
                        else
                        {
                            // 실제 사용한 크기만큼만 복사 (이벤트 핸들러에서 사용)
                            body = new byte[bodyLength];
                            Array.Copy(rentedBuffer, 0, body, 0, bodyLength);
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

    public void Dispose()
    {
        if (_disposed)
            return;

        Disconnect();
        _disposed = true;
    }
}
