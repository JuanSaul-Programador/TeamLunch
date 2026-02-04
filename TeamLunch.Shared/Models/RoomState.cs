namespace TeamLunch.Shared.Models;

public class RoomState
{
    public string RoomCode { get; set; } = string.Empty;
    public string Topic { get; set; } = "Votación General"; 
    public List<LunchOption> Options { get; set; } = new();
    public List<string> Users { get; set; } = new(); 
    public List<ChatMessage> Messages { get; set; } = new(); 
    public bool IsVotingActive { get; set; } = true;
    public string Winner { get; set; } = string.Empty;
    public int TimerSeconds { get; set; } = 0; 
    
    // New Pro Features
    public string CreatorName { get; set; } = "";
    public List<string> TypingUsers { get; set; } = new(); 
}

public enum MessageType
{
    Text,
    Image,
    Audio
}

public class ChatMessage
{
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageType Type { get; set; } = MessageType.Text;
}
