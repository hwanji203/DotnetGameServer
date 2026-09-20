using System.ComponentModel.DataAnnotations;
using GameServer.Entities;

namespace GameServer.DTOs;

public class CreateGuildRequest
{
    [Required(ErrorMessage   = "길드 이름은 필수입니다.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage ="길드 이름은 2~20자까지 가능합니다.")]
    public string Name { get; set; } = null!;
    public GuildJoinMode JoinMode { get; set; } = GuildJoinMode.Auto;
}