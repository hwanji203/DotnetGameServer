namespace GameServer.DTOs;

public class GuildListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JoinMode { get; set; } = string.Empty;
    public int Level { get; set; } 
    public int MemberCount { get; set; }
    public int Capacity { get; set; }
}