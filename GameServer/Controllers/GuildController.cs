using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameServer.DTOs;
using GameServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GuildController : ControllerBase
{
    private readonly GuildService _guildService;
    
    public GuildController(GuildService guildService)
    {
        _guildService = guildService;
    }
    
    //UserId를 JWT 토큰으로부터 꺼내는 메서드
    private int? GetUserId()
    {
        string? sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out int userId) ? userId : null;
    }
    
    //길드 생성 End포인트 Post /api/guild
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuildRequest request)
    {
        if (GetUserId() is not int userId)
        {
            return Unauthorized(); //이런일은 없다. 안전코드임.
        }

        (GuildError error, GuildResponse? guild)
            = await _guildService.CreateAsync(userId, request.Name, request.JoinMode);

        return error switch
        {
            GuildError.None => Ok(guild),
            GuildError.AlreadyInGuild => Conflict(new { message = "이미 길드에 소속되어 있습니다." }),
            GuildError.NameTaken => Conflict(new { message = "이미 사용 중인 길드 이름입니다." }),
            _ => StatusCode(500), //나올 수 없는 에러.
        };
    }
    
    //길드 목록 조회 /api/guild
    [HttpGet]
    public async Task<IActionResult> List()
    {
        return Ok(await _guildService.ListAsync());
    }
    
    //내가 속한 길드의 상세정보 GET /api/guild/me
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        GuildResponse? guildRes = await _guildService.GetMyGuildAsync(userId);

        return guildRes is null
            ? NotFound(new { message = "소속된 길드가 없습니다." })
            : Ok(guildRes);
    }
    
    //길드 자동가입 Post api/guild/{guildId}/join
    [HttpPost("{guildId:int}/join")]
    public async Task<IActionResult> Join(int guildId)
    {
        if (GetUserId() is not int userId)
            return Unauthorized();

        (GuildError error, GuildJoinResponse? guild) = await _guildService.JoinAsync(userId, guildId);

        return error switch
        {
            GuildError.None => Ok(guild),
            GuildError.AlreadyInGuild => Conflict(new { message = "이미 길드에 소속되어 있습니다." }),
            GuildError.GuildNotFound => NotFound(new { message = "존재하지 않는 길드" }),
            GuildError.GuildFull => Conflict(new { message = "길드 정원이 가득 찼습니다." }),
            _ => StatusCode(500)
        };
    }
    
    //길드 탈퇴 POST /api/guild/leave
    [HttpPost("leave")]
    public async Task<IActionResult> Leave()
    {
        if (GetUserId() is not int userId)
            return Unauthorized();
        
        GuildError error = await _guildService.LeaveAsync(userId);

        return error switch
        {
            GuildError.None => Ok(new { message = "길드에서 탈퇴처리되었습니다." }),
            GuildError.NotInGuild => BadRequest(new { message = "소속된 길드가 없습니다." }),
            _ => StatusCode(500)
        };
    }
}