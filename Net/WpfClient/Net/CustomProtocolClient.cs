using Client;
using Client.Enums;
using Client.Interfaces;

using System;
using System.Net.Sockets;

namespace WpfClient.Net
{
    public delegate void LogDelegate(LogType logType, string message);

    public class CustomProtocolClient<Package> : Client.TcpClient
    {
        private readonly ByteStream _receiver;
        private LogDelegate _log;
        private readonly CustomProtocolHeaderFilter _headerFilter;
        private readonly CustomProtocolPackageDispatcher _dispatcher;

        public CustomProtocolClient(
            string address,
            int port,
            LogDelegate log,
            CustomProtocolHeaderFilter headerFilter,
            CustomProtocolPackageDispatcher dispatcher) 
            : base(address, port)
        {
            _log = log;
            _receiver = new ByteStream(1024 * 8, 1024 * 16);
            _headerFilter = headerFilter;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Handle client connecting notification
        /// </summary>
        protected override void OnConnecting()
        {
            _log(LogType.Information, "OnConnecting");
        }

        /// <summary>
        /// Handle client connected notification
        /// </summary>
        protected override void OnConnected()
        {
            _log(LogType.Information, "OnConnected");
        }

        /// <summary>
        /// Handle client disconnecting notification
        /// </summary>
        protected override void OnDisconnecting()
        {
            _log(LogType.Information, "OnDisconnecting");
        }

        /// <summary>
        /// Handle client disconnected notification
        /// </summary>
        protected override void OnDisconnected()
        {
            _log(LogType.Information, "OnDisconnected");
        }

        /// <summary>
        /// Handle buffer received notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        /// <remarks>
        /// Notification is called when another chunk of buffer was received from the server
        /// </remarks>
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _log(LogType.Information, "OnReceived");
            _receiver.Write(buffer, offset, size);
            while (true)
            {
                if (_receiver.Position >= _headerFilter.HeaderSize)
                {
                    int bodySize = _headerFilter.GetBodyLengthFromHeader(_receiver.GetBytes(0, _headerFilter.HeaderSize));
                    int totalSize = _headerFilter.HeaderSize + bodySize;
                    if (_receiver.Position >= totalSize)
                    {
                        Span<byte> packet = _receiver.GetBytes(0, totalSize);
                        var package = _headerFilter.DecodePackage(packet);
                        _dispatcher.Dispatch(package);
                        ByteArrayPool.Release(package.Body);
                        _receiver.Advance(totalSize);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Handle buffer sent notification
        /// </summary>
        /// <param name="sent">Size of sent buffer</param>
        /// <param name="pending">Size of pending buffer</param>
        /// <remarks>
        /// Notification is called when another chunk of buffer was sent to the server.
        /// This handler could be used to send another buffer to the server for instance when the pending size is zero.
        /// </remarks>
        protected override void OnSent(long sent, long pending)
        {
            _log(LogType.Information, "OnSent");
        }

        /// <summary>
        /// Handle empty send buffer notification
        /// </summary>
        /// <remarks>
        /// Notification is called when the send buffer is empty and ready for a new data to send.
        /// This handler could be used to send another buffer to the server.
        /// </remarks>
        protected override void OnEmpty()
        {
            _log(LogType.Information, "OnEmpty");
        }

        /// <summary>
        /// Handle error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        protected override void OnError(SocketError error)
        {
            _log(LogType.Information, "OnError");
        }

        #region dispose
        private bool _disposed = false;

        public override void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            base.Dispose();
        }
        #endregion
    }
}
