using Client.Interfaces;

using MessagePack;

using Microsoft.Extensions.Logging;
using Microsoft.IO;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace WpfClient.Net
{
    public class CustomProtocolPackageDispatcher : IPackageDispatcher<MyPackage>
    {
        private static readonly RecyclableMemoryStreamManager _msManager = new RecyclableMemoryStreamManager();
        private readonly Dictionary<ushort, Action<long, MyPackage>> _handleMap = new Dictionary<ushort, Action<long, MyPackage>>();
        private readonly ICustomProtocolHandler _handler;
        private readonly ILogger _logger;

        public CustomProtocolPackageDispatcher(ICustomProtocolHandler handler, ILogger logger)
        {
            _logger = logger;
            _handler = handler;
            // REQ_RES_TEST_ECHO 101
            _handleMap.Add(101, REQ_ROOM_CHAT);
        }

        public void OnConnected(long uniqueId)
        {
            _handler.OnConnected(uniqueId);
        }

        public void OnConnecting(long uniqueId)
        {
            _handler.OnConnecting(uniqueId);
        }

        public void OnDisconnected(long uniqueId)
        {
            _handler.OnDisconnected(uniqueId);
        }

        public void OnDisconnection(long uniqueId)
        {
            _handler.OnDisconnection(uniqueId);
        }

        public void OnEmpty(long uniqueId)
        {
            _handler.OnEmpty(uniqueId);
        }

        public void OnError(long uniqueId, SocketError error)
        {
            _handler.OnError(uniqueId, error);
        }

        public void OnSent(long uniqueId, long sent, long pending)
        {
            _handler.OnSent(uniqueId, sent, pending);
        }

        public void Dispatch(long clientUniqueId, MyPackage package)
        {
            // Handle package
            if (_handleMap.ContainsKey(package.Id) == true)
            {
                _handleMap[package.Id](clientUniqueId, package);
            }
            else
            {
                _logger.LogError($"Unknown PacketID. packetId:{package.Id}");
            }
        }

        public void REQ_ROOM_CHAT(long clientUniqueId, MyPackage package)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            PKTEcho message = MessagePackSerializer.Deserialize<PKTEcho>(package.Body);
            _handler.EchoRes(clientUniqueId, message);
        }
    }
}
