# SignalR

- [SignalR](#signalr)
  - [VSCode Extention](#vscode-extention)
  - [Message Type](#message-type)
  - [Message](#message)
  - [PostMan](#postman)
  - [Every message 0x1E](#every-message-0x1e)

## VSCode Extention

[insert-unicode](https://marketplace.visualstudio.com/items?itemName=brunnerh.insert-unicode)

## Message Type

```cs
public static class HubProtocolConstants
{
    public const int InvocationMessageType = 1;
    public const int StreamItemMessageType = 2;
    public const int CompletionMessageType = 3;
    public const int StreamInvocationMessageType = 4;
    public const int CancelInvocationMessageType = 5;
    public const int PingMessageType = 6;
    public const int CloseMessageType = 7;
}
```

## Message

```cs
/// <summary>
/// A base class for hub messages representing an invocation.
/// </summary>
public abstract class HubMethodInvocationMessage : HubInvocationMessage
{
    /// <summary>
    /// Gets the target method name.
    /// </summary>
    public string Target { get; }
    /// <summary>
    /// Gets the target method arguments.
    /// </summary>
    public object?[] Arguments { get; }
    /// <summary>
    /// The target methods stream IDs.
    /// </summary>
    public string[]? StreamIds { get; }
}
/// <summary>
/// A hub message representing a non-streaming invocation.
/// </summary>
public class InvocationMessage : HubMethodInvocationMessage {}
```

## PostMan

[Postman supports websocket apis](https://blog.postman.com/postman-supports-websocket-apis/)

[PostMan](https://wadehuang36.medium.com/connect-signalr-apis-with-postman-dce2b0f59c2a)

[Using postman with signalr websockets development](https://trailheadtechnology.com/using-postman-with-signalr-websockets-development/)

## Every message 0x1E

communication and every message needs to have a 0x1E/U+001E

```json
ws://localhost:5003/chatHub
wss://localhost:5004/chatHub
```

```json
{"protocol":"json","version":1}
```

```json
{"type":1, "target":"send", "arguments":["Wade","Hi"]}
```

```json
{"type":1, "target":"send", "arguments":["Wade","Thinks for reading my post"]}
```
