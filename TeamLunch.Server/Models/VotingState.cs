namespace TeamLunch.Server.Models;

using TeamLunch.Shared.Models;

public static class VotingStore
{
    public static Dictionary<string, RoomState> Rooms = new();
}