namespace GameServer.Entities;

public enum DungeonRunStatus
{
    InProgress = 0,
    Completed = 1,
}

/// <summary>
/// 서버측에서 기록하는 던전의 기록을 enter로 시작해서 complete로 끝난다.
/// Users와의 관계를 1:N관계이다. 한명의 유저가 여러번 던전을 도니까.
/// </summary>
public class DungeonRun
{
    public int Id { get; set; }

    public int UserId { get; set; } //이 던전을 돌고 있는 유저
    public User User { get; set; } = null!; //탐색속성으로 FK로 가져올때 사용
    
    public int DungeonId { get; set; }
    public DungeonRunStatus Status { get; set; } =  DungeonRunStatus.InProgress;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    //완료시 확정되는 정산 결과물 칼럼
    public int MonsterKilled { get; set; }
    public long GoldGained { get; set; }
    public long ExpGained { get; set; }
}