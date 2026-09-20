namespace GameServer.Entities;

public enum GuildJoinRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Canceled = 3
}

public class GuildJoinRequest
{
    public int Id { get; set; }
    public int GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    
    //신청한 유저
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public GuildJoinRequestStatus Status { get; set; } = GuildJoinRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecideAt { get; set; }
}