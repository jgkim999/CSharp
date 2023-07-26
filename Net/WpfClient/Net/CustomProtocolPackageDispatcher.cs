using Client.Interfaces;

using MessagePack;

using Microsoft.Extensions.Logging;
using Microsoft.IO;

using System;
using System.Collections.Generic;

namespace WpfClient.Net
{
    public class CustomProtocolPackageDispatcher : IPackageDispatcher<MyPackage>
    {
        private static readonly RecyclableMemoryStreamManager _msManager = new RecyclableMemoryStreamManager();
        private readonly Dictionary<ushort, Action<MyPackage>> _handleMap = new Dictionary<ushort, Action<MyPackage>>();
        private readonly ICustomProtocolHandler _handler;
        private readonly ILogger _logger;

        public CustomProtocolPackageDispatcher(ICustomProtocolHandler handler, ILogger logger)
        {
            _logger = logger;
            _handler = handler;
            // REQ_RES_TEST_ECHO 101
            _handleMap.Add(101, REQ_ROOM_CHAT);
        }

        public void Dispatch(MyPackage package)
        {
            // Handle package
            if (_handleMap.ContainsKey(package.Id) == true)
            {
                _handleMap[package.Id](package);
            }
            else
            {
                _logger.LogError($"Unknown PacketID. packetId:{package.Id}");
            }
        }

        public void REQ_ROOM_CHAT(MyPackage package)
        {
            PKTEcho message = MessagePackSerializer.Deserialize<PKTEcho>(package.Body);
            _handler.EchoRes(message);
        }
    }
}
