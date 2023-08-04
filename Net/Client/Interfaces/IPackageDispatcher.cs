using System.Net.Sockets;

namespace Client.Interfaces
{
    public interface IPackageDispatcher<T>
    {
        void Dispatch(long clientUniqueId, T package);
        void OnConnected(long uniqueId);
        void OnConnecting(long uniqueId);
        void OnDisconnected(long uniqueId);
        void OnDisconnection(long uniqueId);
        void OnEmpty(long uniqueId);
        void OnError(long uniqueId, SocketError error);
        void OnSent(long uniqueId, long sent, long pending);
    }
}
