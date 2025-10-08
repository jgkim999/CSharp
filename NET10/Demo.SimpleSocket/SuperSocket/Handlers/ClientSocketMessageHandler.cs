using System.Collections.Concurrent;
using Bogus;
using Demo.Application.DTO.Socket;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// MessageType별로 메시지 처리 핸들러를 등록하고 실행하는 클래스
/// </summary>
public class ClientSocketMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<BinaryPackageInfo, DemoSession, Task>> _handlers = new();
    private readonly ILogger<ClientSocketMessageHandler> _logger;
    private readonly ICommandMediator _mediator;

    public ClientSocketMessageHandler(ILogger<ClientSocketMessageHandler> logger, ICommandMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
        RegisterHandler(SocketMessageType.Pong, OnPongAsync);
        RegisterHandler(SocketMessageType.MsgPackRequest, OnSocketMsgPackReqAsync);
        RegisterHandler(SocketMessageType.VeryLongReq, OnVeryLongReqAsync);
    }


    /// <summary>
    /// MessageType에 대한 핸들러 등록
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    public void RegisterHandler(SocketMessageType messageType, Func<BinaryPackageInfo, DemoSession, Task> handler)
    {
        RegisterHandler((ushort)messageType, handler);
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록 (ushort 오버로드)
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    public void RegisterHandler(ushort messageType, Func<BinaryPackageInfo, DemoSession, Task> handler)
    {
        if (_handlers.TryAdd(messageType, handler))
        {
            _logger.LogInformation("Handler registered for MessageType: {MessageType}", messageType);
        }
        else
        {
            _logger.LogWarning("Handler already exists for MessageType: {MessageType}. Overwriting.", messageType);
            _handlers[messageType] = handler;
        }
    }

    /// <summary>
    /// 여러 MessageType에 대한 핸들러를 한 번에 등록
    /// </summary>
    public void RegisterHandlers(Dictionary<ushort, Func<BinaryPackageInfo, DemoSession, Task>> handlers)
    {
        foreach (var kvp in handlers)
        {
            RegisterHandler(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// MessageType에 해당하는 핸들러 실행
    /// </summary>
    /// <param name="package">패킷 정보</param>
    /// <param name="session">세션</param>
    /// <returns>핸들러를 찾아 실행했으면 true, 없으면 false</returns>
    public async Task<bool> HandleAsync(BinaryPackageInfo package, DemoSession session)
    {
        if (_handlers.TryGetValue(package.MessageType, out var handler))
        {
            try
            {
                await handler(package, session);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error handling message. SessionId: {SessionId}, MessageType: {MessageType}",
                    session.SessionID, package.MessageType);
                throw;
            }
        }

        return false;
    }

    /// <summary>
    /// MessageType에 대한 핸들러 존재 여부 확인
    /// </summary>
    public bool HasHandler(ushort messageType) => _handlers.ContainsKey(messageType);

    /// <summary>
    /// MessageType에 대한 핸들러 제거
    /// </summary>
    public bool UnregisterHandler(ushort messageType)
    {
        return _handlers.TryRemove(messageType, out _);
    }

    /// <summary>
    /// 등록된 모든 핸들러 제거
    /// </summary>
    public void ClearHandlers()
    {
        _handlers.Clear();
        _logger.LogInformation("All handlers cleared");
    }

    /// <summary>
    /// 등록된 핸들러 개수
    /// </summary>
    public int HandlerCount => _handlers.Count;
    
    private T? CheckPackage<T>(BinaryPackageInfo package, DemoSession session) where T : class
    {
        if (package.BodyLength == 0)
        {
            _logger.LogWarning("Received package with empty body. SessionID: {SessionID}", session.SessionID);
            return null;
        }

        try
        {
            ReadOnlyMemory<byte> bodyMemory = package.Body.AsMemory(0, package.BodyLength);
            byte[]? decryptedData = null;
            byte[]? decompressedData = null;

            try
            {
                // 1단계: 암호화된 데이터인 경우 먼저 복호화
                if (package.Flags.HasFlag(PacketFlags.Encrypted))
                {
                    decryptedData = session.DecryptData(bodyMemory.Span);
                    bodyMemory = decryptedData.AsMemory();
                }

                // 2단계: 압축된 데이터인 경우 압축 해제
                if (package.Flags.HasFlag(PacketFlags.Compressed))
                {
                    decompressedData = session.DecompressData(bodyMemory.Span);
                    bodyMemory = decompressedData.AsMemory();
                }

                // 3단계: MessagePack 역직렬화
                return MessagePack.MessagePackSerializer.Deserialize<T>(bodyMemory);
            }
            finally
            {
                // 할당된 데이터 해제 (GC에 맡김)
                decryptedData = null;
                decompressedData = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing package. SessionID: {SessionID}", session.SessionID);
            return null;
        }
    }
    
    private async Task OnSocketMsgPackReqAsync(BinaryPackageInfo package, DemoSession session)
    {
        var request = CheckPackage<MsgPackReq>(package, session);
        if (request == null)
            return;
        await _mediator.SendAsync(new MsgPackReqCommand(request, session));
    }
    
    private async Task OnPongAsync(BinaryPackageInfo package, DemoSession session)
    {
        var request = CheckPackage<MsgPackPing>(package, session);
        if (request == null)
            return;
        var utcNow = DateTime.UtcNow;
        var rtt = (utcNow - request.ServerDt).TotalMilliseconds;
        session.SetLastPong(utcNow, rtt);
        _logger.LogInformation("Pong 수신. {MilliSeconds}ms", rtt);
        await Task.CompletedTask;
    }

    /// <summary>
    /// VeryLongReq 메시지 처리 핸들러
    /// 압축 테스트를 위해 매우 긴 텍스트를 응답으로 전송
    /// </summary>
    private async Task OnVeryLongReqAsync(BinaryPackageInfo package, DemoSession session)
    {
        var request = CheckPackage<VeryLongReq>(package, session);
        if (request == null)
            return;

        _logger.LogInformation("VeryLongReq 수신. SessionID: {SessionID}, DataLength: {DataLength}, Compressed: {Compressed}",
            session.SessionID, request.Data?.Length ?? 0, package.Flags.HasFlag(PacketFlags.Compressed));

        // Bogus를 사용하여 매우 긴 응답 데이터 생성 (약 2000~3000자)
        var faker = new Faker("ko");
        var longText = string.Join("\n", new[]
        {
            faker.Lorem.Paragraphs(10),  // 10개 문단
            faker.Lorem.Paragraphs(10),  // 10개 문단
            faker.Lorem.Paragraphs(10),  // 10개 문단
        });

        var response = new VeryLongRes
        {
            Data = $"[서버 응답] 수신한 데이터 길이: {request.Data?.Length ?? 0}자\n\n{longText}"
        };

        await session.SendMessagePackAsync(SocketMessageType.VeryLongRes, response);

        _logger.LogInformation("VeryLongRes 전송 완료. SessionID: {SessionID}, ResponseLength: {ResponseLength}",
            session.SessionID, response.Data.Length);
    }
}
