using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.Application.DTO;
using Demo.SimpleSocket.SuperSocket;
using Demo.SimpleSocketClient.Services;

namespace Demo.SimpleSocketClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly SocketClient _socketClient;
    private readonly ClientMessageHandler _messageHandler;

    [ObservableProperty]
    private string _serverIp = "127.0.0.1";

    [ObservableProperty]
    private int _serverPort = 4040;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "연결 안 됨";

    [ObservableProperty]
    private string _messageToSend = string.Empty;

    [ObservableProperty]
    private ushort _messageType = 1;

    [ObservableProperty]
    private string _msgPackName = string.Empty;

    [ObservableProperty]
    private string _msgPackMessage = string.Empty;

    public ObservableCollection<string> ReceivedMessages { get; } = new();

    public MainWindowViewModel()
    {
        _socketClient = new SocketClient();
        _socketClient.MessageReceived += OnMessageReceived;
        _socketClient.Disconnected += OnDisconnected;
        _socketClient.ErrorOccurred += OnErrorOccurred;

        _messageHandler = new ClientMessageHandler();
        RegisterMessageHandlers();
    }

    /// <summary>
    /// MessageType별 핸들러 등록
    /// </summary>
    private void RegisterMessageHandlers()
    {
        // MsgPackResponse 핸들러
        _messageHandler.RegisterHandler(SocketMessageType.MsgPackResponse, OnMsgPackResponse);

        // ConnectionSuccess 핸들러
        _messageHandler.RegisterHandler(SocketMessageType.ConnectionSuccess, OnConnectionSuccessNfy);
    }
    
    private string OnConnectionSuccessNfy(MessageReceivedEventArgs arg)
    {
        try
        {
            var response = arg.DeserializeMessagePack<SocketMsgConnectionSuccessNfy>();
            if (response != null)
            {
                return $"[수신 MsgPack] Type:{arg.MessageType}, Msg: {response.ConnectionId} {response.ServerUtcTime}";
            }
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패";
        }
        catch (Exception ex)
        {
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패: {ex.Message}";
        }
    }

    private string OnMsgPackResponse(MessageReceivedEventArgs arg)
    {
        try
        {
            var response = arg.DeserializeMessagePack<SocketMsgPackRes>();
            if (response != null)
            {
                return $"[수신 MsgPack] Type:{arg.MessageType}, Msg: {response.Msg}, ProcessDt: {response.ProcessDt:yyyy-MM-dd HH:mm:ss}";
            }
            return $"[수신] Type:{arg.MessageType}, Message: {arg.BodyText}";
        }
        catch (Exception ex)
        {
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected)
            return;

        try
        {
            StatusMessage = "연결 중...";
            await _socketClient.ConnectAsync(ServerIp, ServerPort);
            IsConnected = true;
            StatusMessage = $"연결됨: {ServerIp}:{ServerPort}";
            AddReceivedMessage($"[시스템] 서버에 연결되었습니다.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"연결 실패: {ex.Message}";
            AddReceivedMessage($"[오류] 연결 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        if (!IsConnected)
            return;

        try
        {
            _socketClient.Disconnect();
            IsConnected = false;
            StatusMessage = "연결 해제됨";
            AddReceivedMessage($"[시스템] 서버 연결이 해제되었습니다.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"연결 해제 실패: {ex.Message}";
            AddReceivedMessage($"[오류] 연결 해제 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(MessageToSend))
            return;

        try
        {
            await _socketClient.SendTextMessageAsync(MessageType, MessageToSend);
            AddReceivedMessage($"[전송] Type:{MessageType}, Message: {MessageToSend}");
            MessageToSend = string.Empty;
        }
        catch (Exception ex)
        {
            AddReceivedMessage($"[오류] 전송 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SendMsgPackAsync()
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(MsgPackName) || string.IsNullOrWhiteSpace(MsgPackMessage))
            return;

        try
        {
            var request = new SocketMsgPackReq
            {
                Name = MsgPackName,
                Message = MsgPackMessage
            };

            await _socketClient.SendMessagePackAsync(SocketMessageType.MsgPackRequest, request);
            AddReceivedMessage($"[전송 MsgPack] Type:{SocketMessageType.MsgPackRequest}, Name: {MsgPackName}, Message: {MsgPackMessage}");
            MsgPackName = string.Empty;
            MsgPackMessage = string.Empty;
        }
        catch (Exception ex)
        {
            AddReceivedMessage($"[오류] MsgPack 전송 실패: {ex.Message}");
        }
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var message = await _messageHandler.HandleAsync(e);
        AddReceivedMessage(message);
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        IsConnected = false;
        StatusMessage = "연결 해제됨";
        AddReceivedMessage($"[시스템] 서버 연결이 끊어졌습니다.");
    }

    private void OnErrorOccurred(object? sender, Exception e)
    {
        AddReceivedMessage($"[오류] {e.Message}");
    }

    private void AddReceivedMessage(string message)
    {
        // UI 스레드에서 실행
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ReceivedMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            // 최대 100개까지만 유지
            while (ReceivedMessages.Count > 100)
                ReceivedMessages.RemoveAt(ReceivedMessages.Count - 1);
        });
    }

    public void Dispose()
    {
        _socketClient?.Dispose();
    }
}
