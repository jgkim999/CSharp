using System.Net.Sockets;

namespace WpfClient.Net;

public interface ICustomProtocolHandler
{
    void EchoRes(long clientUniqueId, PKTEcho message);
    void OnConnected(long uniqueId);
    void OnConnecting(long uniqueId);
    void OnDisconnected(long uniqueId);
    void OnDisconnection(long uniqueId);
    void OnEmpty(long uniqueId);
    void OnError(long uniqueId, SocketError error);
    void OnSent(long uniqueId, long sent, long pending);
}
