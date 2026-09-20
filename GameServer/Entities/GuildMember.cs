namespace GameServer.Entities;

public enum GuildRole
{
    Master = 0,
    Member = 1,
}

/// <summary>
/// 길드 소속 UserId에 Unique인덱스를 걸어서 한 유저는 반드시 한 길드에만 들어가게 한다.
/// 1:1 구조이지만 피벗테이블 구조를 활용하는 이유는 관계 종속 데이터가 있기 때문이다.
/// 만약 User에게 외래키를 부여하고 관계종속데이터를 넣게 되면, 관심사 분리가 깨진다.
/// 유저는 순수하게 유저 정보에 집중해야하고 1:1 은 유니크키를 통해 보장시킨다.
/// </summary>
public class GuildMember 
{
    public int Id { get; set; }
    public int GuildId { get; set; }
    public Guild Guild { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    //이 멤버가 길드에 기여한 누적 경험치(이것으로 랭킹이 부여된다.)
    public long ContributedExp { get; set; }
}