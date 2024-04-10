using Bogus;

using Client;
using Client.Enums;
using Client.Interfaces;

using MahApps.Metro.Controls;

using MessagePack;

using Microsoft.IO;

using Serilog;
using Serilog.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

using WpfClient.Models;
using WpfClient.Net;

namespace WpfClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, ICustomProtocolHandler
    {
        private readonly Channel<LogModel> _channel = Channel.CreateUnbounded<LogModel>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true,
            });
        private readonly Task _consumer;
        private CancellationTokenSource _cancelTokenSource;
        private readonly ObjectPool<LogModel> _logPool = new ObjectPool<LogModel>(() => new LogModel());
        private long _logCount = 0;

        private static readonly RecyclableMemoryStreamManager _msManager = new RecyclableMemoryStreamManager();
        CustomProtocolClient<MyPackage>? _client;
       
        private ConcurrentDictionary<long, CustomProtocolClient<MyPackage>> _clients = new();
        long _logId = 0;
        private static readonly object _syncRoot = new object();
        private readonly Faker _faker;
        private bool _logDisplay = true;

        private NetworkStatistics _networkStatistics = new();

        private DispatcherTimer _networkStatisticsTimer;

        private long _connectionCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RichTextBox(MyRichTextBox, syncRoot: _syncRoot)
                .CreateLogger();

            _faker = new Faker();

            LogToggle.IsOn = true;

            _cancelTokenSource = new CancellationTokenSource();
            _consumer = LogConsume(_cancelTokenSource.Token);

            IpTextBox.Text = "127.0.0.1";
            PortTextBox.Text = "4040";

            _networkStatisticsTimer = new DispatcherTimer();
            _networkStatisticsTimer.Interval = TimeSpan.FromSeconds(1);
            _networkStatisticsTimer.Tick += new EventHandler(Timer_Tick);
            _networkStatisticsTimer.Start();

            AddLog(LogType.Information, "Program started");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var stat = _networkStatistics.StatSum(1);
                NetworkInTextBox.Text = stat.Sent.ToString();
                NetworkOutTextBox.Text = stat.Received.ToString();

                //
                _connectionCount = _clients.Count;
                if (_client is not null && _client.IsConnected == true)
                {
                    ++_connectionCount;                    
                }
                ConnectionCountTextBox.Text = _connectionCount.ToString();
            }));
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string address = IpTextBox.Text;
            ushort port;
            if (ushort.TryParse(PortTextBox.Text, out port) == false)
            {
                AddLog(LogType.Error, "Invalid Port");
                return;
            }
            if (_client != null)
            {
                _client.Dispose();
            }

            var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger("CustomProtocolPackageDispatcher");

            _client = new CustomProtocolClient<MyPackage>(
                address,
                port,
                AddLog,
                new CustomProtocolHeaderFilter(5),
                new CustomProtocolPackageDispatcher(this, logger),
                _networkStatistics);
            if (_client.ConnectAsync() == false)
            {
                AddLog(LogType.Information, $"{_client.UniqueId} Connect failed");
            }
        }

        private void AddLog(LogType logType, string message)
        {
            if (_logDisplay == false)
                return;
            LogModel log = _logPool.Get();
            log.Id = Interlocked.Increment(ref _logId);
            log.LogType = logType;
            log.Message = message;
            _channel.Writer.WriteAsync(log, _cancelTokenSource.Token);
        }

        private async Task LogConsume(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var state in _channel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (_logDisplay)
                    {
                        await Dispatcher.BeginInvoke(new Action(() =>
                        {                            
                            switch (state.LogType)
                            {
                                case LogType.Verbose:
                                    Log.Verbose(state.Message);
                                    break;
                                case LogType.Debug:
                                    Log.Debug(state.Message);
                                    break;
                                case LogType.Information:
                                    Log.Information(state.Message);
                                    break;
                                case LogType.Warning:
                                    Log.Warning(state.Message);
                                    break;
                                case LogType.Error:
                                    Log.Error(state.Message);
                                    break;
                                case LogType.Fatal:
                                    Log.Fatal(state.Message);
                                    break;
                                default:
                                    Log.Error(state.Message);
                                    break;
                            }
                            Interlocked.Increment(ref _logCount);
                            //MyRichTextBox.ScrollToEnd();
                        }));
                    }
                    _logPool.Return(state);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }

        private void MyRichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            long logCount = Interlocked.Read(ref _logCount);
            if (logCount >= (long)LogSize.Value)
            {
                TextRange tr = new TextRange(MyRichTextBox.Document.ContentStart, MyRichTextBox.Document.ContentEnd);
                tr.Text = string.Empty;
                Interlocked.Exchange(ref _logCount, 0);
            }
            else
            {
                MyRichTextBox.ScrollToEnd();
            }
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            string chatMsg = ChatTextBox.Text;
            if (string.IsNullOrEmpty(chatMsg))
            {
                chatMsg = _faker.Lorem.Sentence(16);
                ChatTextBox.Text = chatMsg;
            }
            SendChat(chatMsg, 1);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client is null)
            {
                return;
            }
            if (_client.IsConnected is false)
            {
                return;
            }
            _client.DisconnectAsync();
        }

        public void EchoRes(long clientUniqueId, PKTEcho message)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            AddLog(LogType.Information, $"{clientUniqueId} EchoRes: {message.Message}");
        }

        private void ChatRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            SendChat(_faker.Lorem.Sentence(16), (int)RepeatCount.Value);
        }

        private void SendChat(string message, int repeatCount)
        {
            PKTReqRoomChat chat = new PKTReqRoomChat()
            {
                ChatMessage = message,
            };
            using (MemoryStream ms = _msManager.GetStream())
            {
                MessagePackSerializer.Serialize(ms, chat);
                //byte[] body = MessagePackSerializer.Serialize(chat);

                byte[] sendData = PacketToBytes.Make(1026, ms);
                for (int i = 0; i < repeatCount; ++i)
                {
                    if (_client?.SendAsync(sendData) == false)
                    {
                        Log.Error($"{_client.UniqueId} Send failed");
                    }
                    foreach (var client in _clients.Values)
                    {
                        client?.SendAsync(sendData);
                    }
                }
                ByteArrayPool.Release(sendData);
            }
        }

        private void LogToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch ts = sender as ToggleSwitch;
            if (ts != null)
            {
                _logDisplay = ts.IsOn;
            }
        }

        public void OnConnected(long uniqueId)
        {
            AddLog(LogType.Information, $"{uniqueId} OnConnected");
        }

        public void OnConnecting(long uniqueId)
        {
            AddLog(LogType.Information, $"{uniqueId} OnConnecting");
        }

        public void OnDisconnected(long uniqueId)
        {
            AddLog(LogType.Information, $"{uniqueId} OnDisconnected");
            _clients.TryRemove(uniqueId, out _);
        }

        public void OnDisconnection(long uniqueId)
        {
            AddLog(LogType.Information, $"{uniqueId} OnDisconnection");
        }

        public void OnEmpty(long uniqueId)
        {
            //AddLog(LogType.Information, $"{uniqueId} OnEmpty");
        }

        public void OnError(long uniqueId, SocketError error)
        {
            AddLog(LogType.Error, $"{uniqueId} OnError {error}");
        }

        public void OnSent(long uniqueId, long sent, long pending)
        {
            //AddLog(LogType.Information, $"{uniqueId} OnSent send:{sent} pending:{pending}");
        }

        private void ConnectMultiButton_Click(object sender, RoutedEventArgs e)
        {
            int clientNum = (int)MultiClientCount.Value;
            if (clientNum <= 0)
            {
                return;
            }
            string address = IpTextBox.Text;
            ushort port;
            if (ushort.TryParse(PortTextBox.Text, out port) == false)
            {
                AddLog(LogType.Error, "Invalid Port");
                return;
            }

            for (int i = 0; i < clientNum; ++i)
            {
                var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger("CustomProtocolPackageDispatcher");

                var client = new CustomProtocolClient<MyPackage>(
                    address,
                    port,
                    AddLog,
                    new CustomProtocolHeaderFilter(5),
                    new CustomProtocolPackageDispatcher(this, logger),
                    _networkStatistics);
                if (client.ConnectAsync() == false)
                {
                    AddLog(LogType.Information, $"{client.UniqueId} Connect failed");
                }
                else
                {
                    _clients.TryAdd(client.UniqueId, client);
                }
            }
        }

        private void MultiCloseButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var client in _clients.Values)
            {
                client.DisconnectAsync();
            }
            _clients.Clear();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            TextRange tr = new TextRange(MyRichTextBox.Document.ContentStart, MyRichTextBox.Document.ContentEnd);
            tr.Text = string.Empty;
        }
    }
}
