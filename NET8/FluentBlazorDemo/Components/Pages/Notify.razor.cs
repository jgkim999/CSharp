using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace FluentBlazorDemo.Components.Pages;

public partial class Notify : ComponentBase, IAsyncDisposable
{
    [Inject] private ILogger<Notify> Logger { get; set; }

    [Inject] private NavigationManager Navigation { get; set; }

    private string message = string.Empty;

    private HubConnection? hubConnection;
    private List<string> messages = new();

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/notifyhub"))
            .Build();

        hubConnection.On<string>("NotifyMessage", message =>
        {
            InvokeAsync(() =>
            {
                messages.Add(message);
                StateHasChanged();
            });
        });

        await hubConnection.StartAsync();
    }

    public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (hubConnection is null || IsConnected == false)
        {
            return;
        }

        try
        {
            await hubConnection.SendAsync("SendMessage", message);
            message = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send message");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}