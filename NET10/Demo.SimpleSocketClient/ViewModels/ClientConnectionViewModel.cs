using System;

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Demo.SimpleSocketClient.Services;
using Demo.SimpleSocketShare;
using Demo.SimpleSocketShare.Messages;

namespace Demo.SimpleSocketClient.ViewModels;

/// <summary>
/// 개별 클라이언트 연결을 관리하는 ViewModel
/// </summary>
public partial class ClientConnectionViewModel : ViewModelBase, IDisposable
{
    private readonly SocketClient _socketClient;
    private readonly ClientMessageHandler _messageHandler;
    private readonly Faker _faker = new();
    private readonly int _clientId;

    [ObservableProperty]
    private string _clientName;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = "연결 안 됨";

    [ObservableProperty]
    private string _msgPackName = string.Empty;

    [ObservableProperty]
    private string _msgPackMessage = string.Empty;

    public ObservableCollection<string> ReceivedMessages { get; } = new();

    public ClientConnectionViewModel(int clientId, string serverIp, int serverPort)
    {
        _clientId = clientId;
        _clientName = $"클라이언트 #{clientId}";

        _socketClient = new SocketClient();
        _socketClient.MessageReceived += OnMessageReceived;
        _socketClient.Disconnected += OnDisconnected;
        _socketClient.ErrorOccurred += OnErrorOccurred;
        _socketClient.LogHandler = AddReceivedMessage;

        _messageHandler = new ClientMessageHandler();
        RegisterMessageHandlers();

        // 자동 연결
        _ = ConnectAsync(serverIp, serverPort);
    }

    /// <summary>
    /// MessageType별 핸들러 등록
    /// </summary>
    private void RegisterMessageHandlers()
    {
        _messageHandler.RegisterHandler(SocketMessageType.ConnectionSuccess, OnConnectionSuccessNfy);
        _messageHandler.RegisterHandler(SocketMessageType.Ping, OnPing);
        _messageHandler.RegisterHandler(SocketMessageType.MsgPackResponse, OnMsgPackResponse);
        _messageHandler.RegisterHandler(SocketMessageType.VeryLongRes, OnVeryLongRes);
    }

    private string OnPing(MessageReceivedEventArgs arg)
    {
        try
        {
            MsgPackPing? ping = arg.DeserializeMessagePack<MsgPackPing>();
            if (ping is null)
            {
                return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패: null";
            }
            var pong = new MsgPackPong()
            {
                ServerDt = ping.ServerDt
            };

            _socketClient.SendMessagePackAsync(SocketMessageType.Pong, pong)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            return $"[수신] Ping: {(DateTime.UtcNow - ping.ServerDt).TotalMilliseconds}ms";
        }
        catch (Exception ex)
        {
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패: {ex.Message}";
        }
    }

    private string OnConnectionSuccessNfy(MessageReceivedEventArgs arg)
    {
        try
        {
            var response = arg.DeserializeMessagePack<MsgConnectionSuccessNfy>();
            if (response != null)
            {
                // AES Key/IV 설정
                if (response.AesKey != null && response.AesKey.Length > 0 &&
                    response.AesIV != null && response.AesIV.Length > 0)
                {
                    _socketClient.SetAesKey(response.AesKey, response.AesIV);
                    return $"[수신 MsgPack] Type:{arg.MessageType}, ConnectionId: {response.ConnectionId}, ServerTime: {response.ServerUtcTime:HH:mm:ss}, AES Key/IV 설정 완료";
                }

                return $"[수신 MsgPack] Type:{arg.MessageType}, ConnectionId: {response.ConnectionId}, ServerTime: {response.ServerUtcTime:HH:mm:ss}";
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
            var response = arg.DeserializeMessagePack<MsgPackRes>();
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

    private string OnVeryLongRes(MessageReceivedEventArgs arg)
    {
        try
        {
            var response = arg.DeserializeMessagePack<VeryLongRes>();
            if (response != null)
            {
                var compressed = arg.IsCompressed ? " [압축됨]" : "";
                var preview = response.Data.Length > 100
                    ? response.Data.Substring(0, 100) + "..."
                    : response.Data;

                return $"[수신 VeryLongRes]{compressed} 길이: {response.Data.Length}자\n미리보기: {preview}";
            }
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패";
        }
        catch (Exception ex)
        {
            return $"[수신 오류] Type:{arg.MessageType}, 역직렬화 실패: {ex.Message}";
        }
    }

    private async Task ConnectAsync(string host, int port)
    {
        try
        {
            StatusMessage = "연결 중...";
            await _socketClient.ConnectAsync(host, port);
            IsConnected = true;
            StatusMessage = $"연결됨: {host}:{port}";
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
    private async Task SendMsgPackAsync()
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(MsgPackName) || string.IsNullOrWhiteSpace(MsgPackMessage))
            return;

        try
        {
            var request = new MsgPackReq
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

    [RelayCommand]
    private async Task SendVeryLongReqAsync()
    {
        if (!IsConnected)
            return;

        try
        {
            var faker = new Faker("ko");
            var longText = string.Join("\n", new[]
            {
                faker.Lorem.Paragraphs(5),
                faker.Lorem.Paragraphs(5),
            });

            var request = new VeryLongReq
            {
                Data = longText
            };

            await _socketClient.SendMessagePackAsync(SocketMessageType.VeryLongReq, request);
            AddReceivedMessage($"[전송 VeryLongReq] 길이: {longText.Length}자 (압축 테스트)");
        }
        catch (Exception ex)
        {
            AddReceivedMessage($"[오류] VeryLongReq 전송 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SendEncryptedMsgPackAsync()
    {
        if (!IsConnected)
            return;

        try
        {
            var request = new MsgPackReq
            {
                Name = $"{ClientName} - 암호화 테스트",
                Message = "AES-256 암호화. " + _faker.Lorem.Sentence(512) 
            };

            await _socketClient.SendMessagePackAsync(SocketMessageType.MsgPackRequest, request, encrypt: true);
            AddReceivedMessage($"[전송 암호화 MsgPack] Type:{SocketMessageType.MsgPackRequest}, Name: {request.Name}, Message: {request.Message}");
        }
        catch (Exception ex)
        {
            AddReceivedMessage($"[오류] 암호화 MsgPack 전송 실패: {ex.Message}");
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