using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using TeamLunch.Shared.Models;

namespace TeamLunch.Client.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    [Inject] public NavigationManager NavManager { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    
    [Parameter] public string? JoinCode { get; set; }

    private enum AppState { Welcome, Lobby, Results }
    private AppState currentState = AppState.Welcome;

    private HubConnection? hubConnection;
    private RoomState roomState = new();
    private string userName = "";
    private string roomCodeInput = "";
    private string newOptionName = "";
    private string topicInput = ""; 
    private string chatInput = "";  
    private bool showQR = false;
    private int timerInput = 0;
    private int countdownSeconds = 0;
    private System.Threading.Timer? countdownTimer;
    private bool showImageDialog = false;
    private string imageUrlInput = "";
    private bool isRecording = false;

    private bool isCreator => roomState.CreatorName == userName;
    private System.Threading.Timer? typingTimer;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(JoinCode))
        {
            roomCodeInput = JoinCode;
        }
        else
        {
            var savedRoom = await JS.InvokeAsync<string>("localStorage.getItem", "teamLunch_roomCode");
            var savedUser = await JS.InvokeAsync<string>("localStorage.getItem", "teamLunch_userName");

            if (!string.IsNullOrWhiteSpace(savedRoom) && !string.IsNullOrWhiteSpace(savedUser))
            {
                roomCodeInput = savedRoom;
                userName = savedUser;
                Snackbar.Add($"¡Bienvenido de vuelta, {userName}!", Severity.Info);
                await JoinRoom();
            }
        }
    }

    private async Task SaveSession()
    {
        await JS.InvokeVoidAsync("localStorage.setItem", "teamLunch_roomCode", roomState.RoomCode);
        await JS.InvokeVoidAsync("localStorage.setItem", "teamLunch_userName", userName);
    }

    private async Task ClearSession()
    {
        await JS.InvokeVoidAsync("localStorage.removeItem", "teamLunch_roomCode");
        await JS.InvokeVoidAsync("localStorage.removeItem", "teamLunch_userName");
    }

    private async Task OnChatInput(ChangeEventArgs e)
    {
        chatInput = e.Value?.ToString() ?? "";
        
         if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("Typing", roomState.RoomCode, userName);
            
            typingTimer?.Dispose();
            typingTimer = new System.Threading.Timer(async _ =>
            {
                if (hubConnection.State == HubConnectionState.Connected)
                    await hubConnection.InvokeAsync("StopTyping", roomState.RoomCode, userName);
                await InvokeAsync(StateHasChanged);
            }, null, 1000, Timeout.Infinite);
        }
    }

    private async Task StartRecording()
    {
        if (hubConnection is null || hubConnection.State != HubConnectionState.Connected)
        {
             Snackbar.Add("Conexión perdida. Intentando reconectar...", Severity.Warning);
             await EnsureConnection();
             if (hubConnection?.State != HubConnectionState.Connected)
             {
                 Snackbar.Add("No se pudo conectar al servidor. Intenta recargar la página.", Severity.Error);
                 return;
             }
        }

        var started = await JS.InvokeAsync<bool>("teamLunchEffects.startRecording");
        if (started)
        {
            isRecording = true;
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("No se pudo acceder al micrófono", Severity.Error);
        }
    }

    private async Task StopRecording()
    {
        if (!isRecording) return;
        
        isRecording = false;
        StateHasChanged(); 
        
        try 
        {
            var audioBase64 = await JS.InvokeAsync<string?>("teamLunchEffects.stopRecording");
            
            if (!string.IsNullOrWhiteSpace(audioBase64))
            {
                if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.InvokeAsync("SendMessage", roomState.RoomCode, userName, audioBase64, MessageType.Audio);
                }
                else
                {
                    Snackbar.Add("Error de conexión al enviar audio. Intentando reconectar...", Severity.Warning);
                    await EnsureConnection();
                    if (hubConnection?.State == HubConnectionState.Connected)
                    {
                         await hubConnection.InvokeAsync("SendMessage", roomState.RoomCode, userName, audioBase64, MessageType.Audio);
                    }
                    else 
                    {
                        Snackbar.Add("No se pudo enviar el audio.", Severity.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recording/sending audio: {ex.Message}");
            Snackbar.Add("Error al procesar el audio", Severity.Error);
        }
    }

    private string GetJoinUrl() => $"{NavManager.BaseUri}join/{roomState.RoomCode}";

    private async Task CopyCodeToClipboard()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", roomState.RoomCode);
        Snackbar.Add("Código copiado", Severity.Success);
    }

    private async Task CopyLinkToClipboard()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", GetJoinUrl());
        Snackbar.Add("Enlace copiado", Severity.Success);
    }

    private async Task PlaySound(string type) => await JS.InvokeVoidAsync("teamLunchEffects.playSound", type);
    private async Task FireConfetti() => await JS.InvokeVoidAsync("teamLunchEffects.fireConfetti");
    private async Task ShowNotification(string title, string body) => await JS.InvokeVoidAsync("teamLunchEffects.showNotification", title, body);

    private async Task EnsureConnection()
    {
        if (hubConnection is null)
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavManager.ToAbsoluteUri("/voting-hub"))
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<RoomState>("UpdateRoomState", async (newState) =>
            {
                var hadVotes = roomState.Options.Sum(o => o.Votes);
                var newVotes = newState.Options.Sum(o => o.Votes);
                
                if (newVotes > hadVotes)
                {
                    await PlaySound("vote");
                }
                
                roomState = newState;
                
                if (!roomState.IsVotingActive && !string.IsNullOrEmpty(roomState.Winner))
                {
                    currentState = AppState.Results;
                    await FireConfetti();
                    await PlaySound("winner");
                }
                await InvokeAsync(StateHasChanged);
            });
            
            hubConnection.On<ChatMessage>("ReceiveMessage", (msg) =>
            {
                roomState.Messages.Add(msg);
                InvokeAsync(StateHasChanged);
            });

            hubConnection.On("RoomNotFound", () =>
            {
                Snackbar.Add("La sala no existe", Severity.Error);
                currentState = AppState.Welcome;
                StateHasChanged();
            });
        }
        
        if (hubConnection.State == HubConnectionState.Disconnected)
        {
            await hubConnection.StartAsync();
        }
    }

    private async Task CreateRoom()
    {
        await EnsureConnection();
        var code = await hubConnection.InvokeAsync<string>("CreateRoom", userName, topicInput, timerInput);
        roomCodeInput = code; 
        await JoinRoom(); 
    }

    private async Task JoinRoom()
    {
        await EnsureConnection();
        await hubConnection.InvokeAsync("JoinRoom", roomCodeInput, userName);
        await SaveSession(); 
        await PlaySound("join");
        currentState = AppState.Lobby;
        
        if (roomState.TimerSeconds > 0)
        {
            StartCountdown(roomState.TimerSeconds);
        }
    }

    private void StartCountdown(int seconds)
    {
        countdownSeconds = seconds;
        countdownTimer?.Dispose();
        countdownTimer = new System.Threading.Timer(async _ =>
        {
            if (countdownSeconds > 0)
            {
                countdownSeconds--;
                await InvokeAsync(StateHasChanged);
                
                if (countdownSeconds == 0)
                {
                    if (isCreator)
                    {
                        await StopVoting();
                    }
                }
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private async Task Vote(string optionName)
    {
        await EnsureConnection();
        if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("Vote", roomState.RoomCode, optionName, userName);
        }
    }

    private async Task AddOption()
    {
        if (!string.IsNullOrWhiteSpace(newOptionName))
        {
            await EnsureConnection();
            if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
            {
                await hubConnection.InvokeAsync("AddOption", roomState.RoomCode, newOptionName);
                newOptionName = "";
            }
        }
    }

    private async Task StopVoting()
    {
        if (!isCreator) return;

        await EnsureConnection();
        if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.InvokeAsync("StopVoting", roomState.RoomCode);
        }
    }
    
    private async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(chatInput))
        {
            await EnsureConnection();
            if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
            {
                await hubConnection.InvokeAsync("SendMessage", roomState.RoomCode, userName, chatInput, MessageType.Text);
                chatInput = "";
                await hubConnection.InvokeAsync("StopTyping", roomState.RoomCode, userName);
            }
        }
    }

    private async Task ResetApp()
    {
        if (!isCreator) return;
        
        await ClearSession();
        
        currentState = AppState.Welcome;
        roomCodeInput = "";
        roomState = new RoomState();
        topicInput = "";
        countdownTimer?.Dispose();
        countdownSeconds = 0;
    }

    private async Task Unvote(string optionName)
    {
        await EnsureConnection();
        if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected) 
        {
            await hubConnection.InvokeAsync("Unvote", roomState.RoomCode, optionName, userName);
        }
    }

    private async Task SendImageMessage()
    {
        if (!string.IsNullOrWhiteSpace(imageUrlInput))
        {
            await EnsureConnection();
            if (hubConnection is not null && hubConnection.State == HubConnectionState.Connected)
            {
                await hubConnection.InvokeAsync("SendMessage", roomState.RoomCode, userName, imageUrlInput, MessageType.Image);
                imageUrlInput = "";
                showImageDialog = false;
            }
        }
    }

    private async Task OpenImage(string url)
    {
        await JS.InvokeVoidAsync("window.open", url, "_blank");
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
        countdownTimer?.Dispose();
    }
}
