namespace TeamLunch.Shared.Models;

public class LunchOption
{
    public string Name { get; set; } = string.Empty;
    public int Votes { get; set; }
    public List<string> Voters { get; set; } = new(); 
}
