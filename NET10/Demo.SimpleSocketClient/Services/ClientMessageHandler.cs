using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Demo.SimpleSocket.SuperSocket;

namespace Demo.SimpleSocketClient.Services;

/// <summary>
/// 클라이언트 메시지 핸들러 - MessageType별로 처리 함수를 등록하고 실행
/// </summary>
public class ClientMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<MessageReceivedEventArgs, Task<string>>> _handlers = new();

    /// <summary>
    /// MessageType에 대한 핸들러 등록
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 - 포맷된 메시지 문자열을 반환</param>
    public void RegisterHandler(ushort messageType, Func<MessageReceivedEventArgs, Task<string>> handler)
    {
        _handlers[messageType] = handler;
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록 (동기 버전)
    /// </summary>
    public void RegisterHandler(SocketMessageType messageType, Func<MessageReceivedEventArgs, string> handler)
    {
        _handlers[(ushort)messageType] = args => Task.FromResult(handler(args));
    }

    /// <summary>
    /// 등록된 핸들러를 통해 메시지 처리
    /// </summary>
    /// <param name="args">수신된 메시지 정보</param>
    /// <returns>포맷된 메시지 문자열</returns>
    public async Task<string> HandleAsync(MessageReceivedEventArgs args)
    {
        if (_handlers.TryGetValue(args.MessageType, out var handler))
        {
            try
            {
                return await handler(args);
            }
            catch (Exception ex)
            {
                return $"[수신 오류] Type:{args.MessageType}, 처리 실패: {ex.Message}";
            }
        }

        // 기본 핸들러 - 등록되지 않은 MessageType
        return $"[수신] Type:{args.MessageType}, Message: {args.BodyText}";
    }

    /// <summary>
    /// 핸들러 존재 여부 확인
    /// </summary>
    public bool HasHandler(ushort messageType) => _handlers.ContainsKey(messageType);

    /// <summary>
    /// 핸들러 제거
    /// </summary>
    public bool UnregisterHandler(ushort messageType) => _handlers.TryRemove(messageType, out _);

    /// <summary>
    /// 모든 핸들러 제거
    /// </summary>
    public void ClearHandlers() => _handlers.Clear();

    /// <summary>
    /// 등록된 핸들러 개수
    /// </summary>
    public int HandlerCount => _handlers.Count;
}