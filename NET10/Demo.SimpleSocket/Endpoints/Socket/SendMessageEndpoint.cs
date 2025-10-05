using System.Buffers.Binary;
using System.Net.Sockets;
using FastEndpoints;

namespace Demo.SimpleSocket.Endpoints.Socket;

public class SendMessageEndpointSummary : Summary<SendMessageEndpoint>
{
    public SendMessageEndpointSummary()
    {
        Summary = "TCP socket";
        Description = "This endpoint sends a message to the server via a TCP socket";
        Response<SendMessageResponse>(200, "Message sent successfully");
        Response(400, "Invalid Base64 message format");
        Response(500, "An error occurred while sending the message");
    }
}

/// <summary>
/// TCP 소켓으로 메시지를 전송하고 응답을 받는 엔드포인트
/// </summary>
public class SendMessageEndpoint : Endpoint<SendMessageRequest, SendMessageResponse>
{
    private readonly ILogger<SendMessageEndpoint> _logger;
    private readonly IConfiguration _configuration;

    public SendMessageEndpoint(ILogger<SendMessageEndpoint> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("/api/socket/send");
        AllowAnonymous();
        Group<SocketGroup>();
        Summary(new SendMessageEndpointSummary());
    }

    public override async Task HandleAsync(SendMessageRequest req, CancellationToken ct)
    {
        try
        {
            // Base64 디코딩
            byte[] messageBody;
            try
            {
                messageBody = Convert.FromBase64String(req.Message);
            }
            catch (FormatException)
            {
                HttpContext.Response.StatusCode = 500;
                Response = new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid Base64 message format"
                };
                return;
            }

            // 패킷 구성: MessageType(2) + BodyLength(2) + Body
            ushort bodyLength = (ushort)messageBody.Length;
            byte[] packet = new byte[4 + bodyLength];

            // MessageType을 BigEndian으로 쓰기
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), req.MessageType);

            // BodyLength를 BigEndian으로 쓰기
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2, 2), bodyLength);

            // Body 복사
            if (bodyLength > 0)
            {
                Array.Copy(messageBody, 0, packet, 4, bodyLength);
            }

            // TCP 연결 및 메시지 전송
            var host = _configuration["serverOptions:listeners:0:ip"] ?? "127.0.0.1";
            var port = int.Parse(_configuration["serverOptions:listeners:0:port"] ?? "4040");

            // "Any"는 localhost로 변환
            if (host == "Any" || host == "0.0.0.0")
            {
                host = "127.0.0.1";
            }

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, ct);

            await using var stream = client.GetStream();

            // 연결 성공 패킷 수신 (MessageType: 0xFFFF, BodyLength: 0)
            byte[] connectHeaderBuffer = new byte[4];
            int connectBytesRead = await stream.ReadAsync(connectHeaderBuffer, ct);

            if (connectBytesRead < 4)
            {
                HttpContext.Response.StatusCode = 500;
                Response = new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to receive connection success packet: {connectBytesRead} bytes"
                };
                return;
            }

            ushort connectMessageType = BinaryPrimitives.ReadUInt16BigEndian(connectHeaderBuffer.AsSpan(0, 2));
            _logger.LogInformation("Received connection packet - MessageType: {MessageType}", connectMessageType);

            // 패킷 전송
            await stream.WriteAsync(packet, ct);
            _logger.LogInformation(
                "Sent packet - MessageType: {MessageType}, BodyLength: {BodyLength}",
                req.MessageType, bodyLength);

            // ECHO 응답 수신 (헤더 4바이트)
            byte[] headerBuffer = new byte[4];
            int headerBytesRead = await stream.ReadAsync(headerBuffer, ct);

            if (headerBytesRead < 4)
            {
                HttpContext.Response.StatusCode = 500;
                Response = new SendMessageResponse
                {
                    Success = false,
                    ErrorMessage = $"Incomplete header received: {headerBytesRead} bytes"
                };
                return;
            }

            // 헤더 파싱
            ushort responseMessageType = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(0, 2));
            ushort responseBodyLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(2, 2));

            _logger.LogInformation(
                "Received response header - MessageType: {MessageType}, BodyLength: {BodyLength}",
                responseMessageType, responseBodyLength);

            // 바디 수신
            byte[] bodyBuffer = new byte[responseBodyLength];
            if (responseBodyLength > 0)
            {
                int totalBytesRead = 0;
                while (totalBytesRead < responseBodyLength)
                {
                    int bytesRead = await stream.ReadAsync(
                        bodyBuffer.AsMemory(totalBytesRead, responseBodyLength - totalBytesRead), ct);

                    if (bytesRead == 0)
                    {
                        HttpContext.Response.StatusCode = 500;
                        Response = new SendMessageResponse
                        {
                            Success = false,
                            ErrorMessage = $"Connection closed before receiving complete body. Expected: {responseBodyLength}, Received: {totalBytesRead}"
                        };
                        return;
                    }

                    totalBytesRead += bytesRead;
                }
            }

            // 성공 응답
            Response = new SendMessageResponse
            {
                Success = true,
                MessageType = responseMessageType,
                BodyLength = responseBodyLength,
                ResponseMessage = Convert.ToBase64String(bodyBuffer)
            };
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Socket error while sending message");
            HttpContext.Response.StatusCode = 500;
            Response = new SendMessageResponse
            {
                Success = false,
                ErrorMessage = $"Socket error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending message");
            HttpContext.Response.StatusCode = 500;
            Response = new SendMessageResponse
            {
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}"
            };
        }
    }
}
