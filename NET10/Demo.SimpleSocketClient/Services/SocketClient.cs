using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Demo.Application.Utils;
using Demo.SimpleSocket.SuperSocket;
using MessagePack;

namespace Demo.SimpleSocketClient.Services;

/// <summary>
/// SimpleSocket 서버와 통신하는 TCP 클라이언트
/// 프로토콜: 8바이트 헤더(플래그 1 + 시퀀스 2 + 예약 1 + 메시지타입 2 + 길이 2) + 바디
/// </summary>
public class SocketClient : IDisposable
{
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
    /// </summary>
    private async Task SendMessageAsync(ushort messageType, byte[] body, PacketFlags flags, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("서버에 연결되어 있지 않습니다.");

        var bodyLength = (ushort)body.Length;
        var packet = new byte[8 + bodyLength];
        
        // 헤더 작성 (8바이트)
        packet[0] = (byte)flags;  // 플래그 (1바이트)
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1, 2), _sendSequence.GetNext());  // 시퀀스 (2바이트)
        packet[3] = _faker.Random.Byte(1, 255);  // 예약 (1바이트)
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4, 2), messageType);  // 메시지 타입 (2바이트)
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(6, 2), bodyLength);   // 바디 길이 (2바이트)

        // 바디 복사
        if (bodyLength > 0)
            body.CopyTo(packet.AsSpan(8));

        await _stream.WriteAsync(packet, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
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
    /// </summary>
    private Task SendMessagePackAsync<T>(ushort messageType, T obj, CancellationToken cancellationToken = default)
    {
        var body = MessagePackSerializer.Serialize(obj);
        return SendMessageAsync(messageType, body, cancellationToken);
    }

    /// <summary>
    /// 메시지 전송 (ushort 버전)
    /// </summary>
    private Task SendMessageAsync(ushort messageType, byte[] body, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(messageType, body, PacketFlags.None, cancellationToken);
    }

    /// <summary>
    /// 수신 루프
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var headerBuffer = new byte[8];

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

                // 바디 수신
                byte[] body;
                if (bodyLength > 0)
                {
                    body = new byte[bodyLength];
                    var bodyBytesRead = 0;

                    while (bodyBytesRead < bodyLength)
                    {
                        var read = await _stream.ReadAsync(
                            body.AsMemory(bodyBytesRead, bodyLength - bodyBytesRead),
                            cancellationToken);

                        if (read == 0)
                        {
                            Disconnect();
                            return;
                        }

                        bodyBytesRead += read;
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
