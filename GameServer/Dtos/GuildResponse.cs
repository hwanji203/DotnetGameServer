using GameServer.Entities;
using GameServer.Services;

namespace GameServer.DTOs;

public class GuildMemberResponse
{
    public int UserId { get; set; }
    public string NickName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }

    public static GuildMemberResponse From(GuildMember member)
        => new()
        {
            UserId = member.UserId,
            NickName = member?.User?.Nickname ?? string.Empty,
            Role = member.Role.ToString(),
            JoinedAt = member.JoinedAt
        };
}

public class GuildResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string JoinMode { get; set; } = "";
    public int Level { get; set; }
    public long Exp { get; set; }
    public int MemberCount { get; set; }
    public int Capacity { get; set; }
    public List<GuildMemberResponse> Members { get; set; } = new();

    public static GuildResponse From(Guild guild) => new GuildResponse()
    {
        Id = guild.Id,
        Name = guild.Name,
        JoinMode = guild.JoinMode.ToString(),
        Level = guild.Level,
        Exp = guild.Exp,
        MemberCount = guild.Members.Count,
        Capacity = GuildRules.BaseCapacity,
        Members = guild.Members.OrderBy(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(GuildMemberResponse.From)
            .ToList()
    };
}