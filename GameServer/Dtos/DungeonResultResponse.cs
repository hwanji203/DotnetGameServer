namespace GameServer.DTOs;

public class DungeonResultResponse
{
    public int DungeonId { get; set; }
    public int MonsterKilled { get; set; }
    public long GoldGained { get; set; }
    public long ExpGained { get; set; }
    public long TotalGold { get; set;  } //정산 완료하고 굳이 DB에 다시 요ㅕ청 안해도 UI 갱신할 수 있게
    public CharacterResponse Character { get; set; } = null!;
}