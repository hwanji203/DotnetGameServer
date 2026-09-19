namespace GameServer.DTOs;

public class MonsterKill
{
    public int MonsterId { get; set; }
    public int Count { get; set; }
}

public class DungeonCompleteRequests
{
    public List<MonsterKill> Kills { get; set; } = new();
}