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
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Documents;

using WpfClient.Models;
using WpfClient.Net;

namespace WpfClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, ICustomProtocolHandler
    {
        private static readonly RecyclableMemoryStreamManager _msManager = new RecyclableMemoryStreamManager();
        CustomProtocolClient<MyPackage>? _client;
        long _logId = 0;
        private static readonly object _syncRoot = new object();
        private readonly Faker _faker;
        private int _chatRepeatCount = 0;
        private bool _logDisplay = true;

        public MainWindow()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RichTextBox(MyRichTextBox, syncRoot: _syncRoot)
                .CreateLogger();

            _faker = new Faker();

            logToggle.IsOn = true;

            ipTextBox.Text = "127.0.0.1";
            portTextBox.Text = "4040";
            AddLog(LogType.Information, "Program started");
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            string address = ipTextBox.Text;
            ushort port;
            if (ushort.TryParse(portTextBox.Text, out port) == false)
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
                new CustomProtocolPackageDispatcher(this, logger));
            if (_client.ConnectAsync() == false)
            {
                AddLog(LogType.Information, "Connect failed");
            }
        }

        private void AddLog(LogType logType, string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_logDisplay)
                {
                    Interlocked.Increment(ref _logId);
                    /*
                    if (logListView.Items.Count > 100)
                    {
                        logListView.Items.RemoveAt(0);
                    }
                    logListView.Items.Add(new LogModel { Id = _logId, Message = message });
                    logListView.SelectedIndex = logListView.Items.Count - 1;
                    logListView.ScrollIntoView(logListView.SelectedItem);
                    */

                    int maxTextLength = (int)LogSize.Value;
                    TextRange tr = new TextRange(MyRichTextBox.Document.ContentStart, MyRichTextBox.Document.ContentEnd);
                    if (tr.Text.Length > maxTextLength)
                        tr.Text = string.Empty;

                    switch (logType)
                    {
                        case LogType.Verbose:
                            Log.Verbose(message);
                            break;
                        case LogType.Debug:
                            Log.Debug(message);
                            break;
                        case LogType.Information:
                            Log.Information(message);
                            break;
                        case LogType.Warning:
                            Log.Warning(message);
                            break;
                        case LogType.Error:
                            Log.Error(message);
                            break;
                        case LogType.Fatal:
                            Log.Fatal(message);
                            break;
                        default:
                            Log.Error(message);
                            break;
                    }
                    //MyRichTextBox.ScrollToEnd();
                }
            }));
        }

        private void chatButton_Click(object sender, RoutedEventArgs e)
        {
            string chatMsg = chatTextBox.Text;
            if (string.IsNullOrEmpty(chatMsg) )
            {                
                chatMsg = _faker.Lorem.Sentence(16);
                chatTextBox.Text = chatMsg;
            }
            SendChat(chatMsg);
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
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

        public void EchoRes(PKTEcho message)
        {
            AddLog(LogType.Information, $"EchoRes: {message.Message}");
            --_chatRepeatCount;
            if (_chatRepeatCount > 0)
            {
                SendChat(_faker.Lorem.Sentence(16));
            }
        }

        private void chatRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _chatRepeatCount = (int)repeatCount.Value;
            SendChat(_faker.Lorem.Sentence(16));
        }

        private void SendChat(string message)
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
                if (_client?.SendAsync(sendData) == false)
                {
                    Log.Error("Send failed");
                }
                ByteArrayPool.Release(sendData);
            }
        }

        private void logToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch ts = sender as ToggleSwitch;
            if (ts != null)
            {
                _logDisplay = ts.IsOn;
            }
        }

        private void MyRichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //MyRichTextBox.CaretPosition = MyRichTextBox.Document.ContentEnd;
            MyRichTextBox.ScrollToEnd();
        }
    }
}
