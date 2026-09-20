namespace GameServer.Entities;

public enum GuildJoinMode
{
    Auto = 0, // 누구나 가입하는 길드
    Approval = 1, //길드장 승인이 필요한 길드
}
public class Guild
{
    public int Id { get; set; }
    //길드의 고유 이름이고 Unique해야 한다.
    public string Name { get; set; } = null!;
    public GuildJoinMode JoinMode { get; set; } = GuildJoinMode.Auto;
    
    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    
    //길드 레벨이 오를 시 얻을 수 있는 특전 포인트
    public int PerkPoints { get; set; } = 0;
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public List<GuildMember> Members { get; set; } = new();
}
