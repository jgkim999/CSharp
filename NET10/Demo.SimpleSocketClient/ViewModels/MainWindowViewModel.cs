using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Demo.SimpleSocketClient.ViewModels;

/// <summary>
/// 다중 클라이언트 연결을 관리하는 MainWindowViewModel
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private int _nextClientId = 1;

    [ObservableProperty]
    private string _serverIp = "127.0.0.1";

    [ObservableProperty]
    private int _serverPort = 4040;

    [ObservableProperty]
    private int _clientCount = 1;

    [ObservableProperty]
    private ClientConnectionViewModel? _selectedClient;

    public ObservableCollection<ClientConnectionViewModel> Clients { get; } = new();

    public MainWindowViewModel()
    {
    }

    /// <summary>
    /// 단일 클라이언트 추가
    /// </summary>
    [RelayCommand]
    private void AddClient()
    {
        try
        {
            var client = new ClientConnectionViewModel(_nextClientId++, ServerIp, ServerPort);
            Clients.Add(client);

            // 첫 번째 클라이언트는 자동 선택
            if (Clients.Count == 1)
            {
                SelectedClient = client;
            }
        }
        catch (Exception ex)
        {
            // 에러 처리 (필요시 로깅)
            Console.WriteLine($"클라이언트 추가 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 여러 클라이언트 추가
    /// </summary>
    [RelayCommand]
    private void AddMultipleClients()
    {
        if (ClientCount < 1 || ClientCount > 100)
        {
            Console.WriteLine("클라이언트 개수는 1~100 사이여야 합니다.");
            return;
        }

        try
        {
            for (int i = 0; i < ClientCount; i++)
            {
                var client = new ClientConnectionViewModel(_nextClientId++, ServerIp, ServerPort);
                Clients.Add(client);
            }

            // 첫 번째 클라이언트 자동 선택
            if (SelectedClient == null && Clients.Count > 0)
            {
                SelectedClient = Clients[0];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"다중 클라이언트 추가 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 선택된 클라이언트 제거
    /// </summary>
    [RelayCommand]
    private void RemoveClient()
    {
        if (SelectedClient == null)
            return;

        try
        {
            var client = SelectedClient;
            var index = Clients.IndexOf(client);

            // 다음 클라이언트 선택
            if (index >= 0 && Clients.Count > 1)
            {
                SelectedClient = index < Clients.Count - 1
                    ? Clients[index + 1]
                    : Clients[index - 1];
            }
            else
            {
                SelectedClient = null;
            }

            Clients.Remove(client);
            client.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"클라이언트 제거 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 모든 클라이언트 제거
    /// </summary>
    [RelayCommand]
    private void RemoveAllClients()
    {
        try
        {
            foreach (var client in Clients.ToList())
            {
                client.Dispose();
            }
            Clients.Clear();
            SelectedClient = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모든 클라이언트 제거 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 모든 클라이언트 연결 해제
    /// </summary>
    [RelayCommand]
    private void DisconnectAllClients()
    {
        try
        {
            foreach (var client in Clients)
            {
                if (client.IsConnected)
                {
                    client.DisconnectCommand.Execute(null);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모든 클라이언트 연결 해제 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 모든 클라이언트에서 MsgPack 전송
    /// </summary>
    [RelayCommand]
    private void SendMsgPackToAll()
    {
        try
        {
            foreach (var client in Clients.Where(c => c.IsConnected))
            {
                client.MsgPackName = "일괄 전송";
                client.MsgPackMessage = $"모든 클라이언트에서 전송 - {DateTime.Now:HH:mm:ss}";
                client.SendMsgPackCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모든 클라이언트 MsgPack 전송 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 모든 클라이언트에서 VeryLongReq 전송
    /// </summary>
    [RelayCommand]
    private void SendVeryLongReqToAll()
    {
        try
        {
            foreach (var client in Clients.Where(c => c.IsConnected))
            {
                client.SendVeryLongReqCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모든 클라이언트 VeryLongReq 전송 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 모든 클라이언트에서 암호화된 MsgPack 전송
    /// </summary>
    [RelayCommand]
    private void SendEncryptedMsgPackToAll()
    {
        try
        {
            foreach (var client in Clients.Where(c => c.IsConnected))
            {
                client.SendEncryptedMsgPackCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모든 클라이언트 암호화 MsgPack 전송 실패: {ex.Message}");
        }
    }

    public void Dispose()
    {
        foreach (var client in Clients)
        {
            client?.Dispose();
        }
        Clients.Clear();
    }
}