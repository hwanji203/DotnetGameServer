namespace GameServer.DTOs;

public class GuildJoinResponse
{
    public string Status { get; set; } = "";
    public GuildResponse? Guild { get; set; }
    public int? RequestId { get; set; }

    public static GuildJoinResponse Joined(GuildResponse guild)
        => new GuildJoinResponse
        {
            Status = "Joined",
            Guild = guild
        };

    public static GuildJoinResponse Pending(int requestId)
        => new GuildJoinResponse
        {
            Status = "Pending",
            RequestId = requestId
        };
}

//이건 길드장이 조회할 목록용 DTO
public class GuildJoinRequestItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string NickName { get; set; } = "";
    public DateTime RequestedAt { get; set; }
}