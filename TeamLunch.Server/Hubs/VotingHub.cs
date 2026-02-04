using Microsoft.AspNetCore.SignalR;
using TeamLunch.Server.Models;
using TeamLunch.Shared.Models;

namespace TeamLunch.Server.Hubs;

public class VotingHub : Hub
{
    public async Task JoinRoom(string roomCode, string userName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState))
        {
            if (!roomState.Users.Contains(userName))
            {
                roomState.Users.Add(userName);
            }
            await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
        }
        else
        {
            await Clients.Caller.SendAsync("RoomNotFound");
        }
    }

    public async Task<string> CreateRoom(string creatorName, string topic, int timerSeconds = 0)
    {
        var roomCode = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        
        var newState = new RoomState
        {
            RoomCode = roomCode,
            Topic = string.IsNullOrWhiteSpace(topic) ? "Votación General" : topic,
            Options = new List<LunchOption>(), 
            Users = new List<string> { creatorName },
            IsVotingActive = true,
            TimerSeconds = timerSeconds,
            CreatorName = creatorName
        };

        VotingStore.Rooms[roomCode] = newState;
        
        VotingStore.Rooms[roomCode] = newState;
        
        return roomCode;
    }
    
    public async Task Typing(string roomCode, string userName)
    {
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState))
        {
            if (!roomState.TypingUsers.Contains(userName))
            {
                roomState.TypingUsers.Add(userName);
                await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
            }
        }
    }

    public async Task StopTyping(string roomCode, string userName)
    {
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState))
        {
            if (roomState.TypingUsers.Contains(userName))
            {
                roomState.TypingUsers.Remove(userName);
                await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
            }
        }
    }
    
    public async Task Vote(string roomCode, string optionName, string userName)
    {
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState) && roomState.IsVotingActive)
        {
            var option = roomState.Options.FirstOrDefault(o => o.Name == optionName);
            if (option != null)
            {
                if (!option.Voters.Contains(userName))
                {
                    option.Voters.Add(userName);
                    option.Votes++;
                    await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
                }
            }
        }
    }

    public async Task Unvote(string roomCode, string optionName, string userName)
    {
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState) && roomState.IsVotingActive)
        {
            var option = roomState.Options.FirstOrDefault(o => o.Name == optionName);
            if (option != null && option.Voters.Contains(userName))
            {
                option.Voters.Remove(userName);
                option.Votes--;
                await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
            }
        }
    }

    public async Task AddOption(string roomCode, string newOptionName)
    {
        if (string.IsNullOrWhiteSpace(newOptionName)) return;

        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState) && roomState.IsVotingActive)
        {
            if (!roomState.Options.Any(o => o.Name.Equals(newOptionName, StringComparison.OrdinalIgnoreCase)))
            {
                roomState.Options.Add(new LunchOption { Name = newOptionName, Votes = 0 });
                await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
            }
        }
    }

    public async Task StopVoting(string roomCode)
    {
        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState))
        {
            roomState.IsVotingActive = false;
            
            var maxVotes = roomState.Options.Any() ? roomState.Options.Max(o => o.Votes) : 0;
            
            if (maxVotes == 0)
            {
                roomState.Winner = "Nadie (Sin votos) ️";
            }
            else
            {
                var winners = roomState.Options.Where(o => o.Votes == maxVotes).Select(o => o.Name).ToList();
                if (winners.Count > 1)
                {
                    roomState.Winner = $"Empate: {string.Join(", ", winners)}";
                }
                else
                {
                    roomState.Winner = winners.First();
                }
            }

            await Clients.Group(roomCode).SendAsync("UpdateRoomState", roomState);
        }
    }

    public async Task SendMessage(string roomCode, string userName, string message, MessageType messageType = MessageType.Text)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        if (VotingStore.Rooms.TryGetValue(roomCode, out var roomState))
        {
            var chatMsg = new ChatMessage
            {
                UserName = userName,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Type = messageType
            };
            
            roomState.Messages.Add(chatMsg);
            
            if (roomState.Messages.Count > 50)
            {
                roomState.Messages.RemoveAt(0);
            }
            
            await Clients.Group(roomCode).SendAsync("ReceiveMessage", chatMsg);
        }
    }
}