using System.Security.Claims;
using GameServer.DTOs;
using GameServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GameServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DungeonController : ControllerBase
{
    private readonly DungeonService _dungeonService;

    public DungeonController(DungeonService dungeonService) { _dungeonService = dungeonService; }

    /// <summary>
    /// /api/dungeon/{dungeonId}/enter
    /// </summary>
    [HttpPost("{dungeonId:int}/enter")]
    public async Task<IActionResult> Enter(int dungeonId)
    {
        if (GetUserId() is not int userId)
        {
            return Unauthorized(); //토큰이 잘못되어 유저아이디가 올바르게 나오지 않는거지
        }

        DungeonEnterResponse? response = await _dungeonService.EnterDungeonAsync(userId, dungeonId);

        if (response is null)
            return NotFound(new { message = "존재하지 않는 던전입니다." });

        return Ok(response);
    }

    // /api/dungeon/{runId}/complete
    [HttpPost("{runId:int}/complete")]
    public async Task<ActionResult> Complete(int runId, [FromBody] DungeonCompleteRequests request)
    {
        if (GetUserId() is not int userId)
        {
            return Unauthorized(); //토큰이 잘못되어 유저아이디가 올바르게 나오지 않는거지
        }

        (DungeonError error, DungeonResultResponse? result)
            = await _dungeonService.CompleteAsync(userId, runId, request);

        return error switch
        {
            DungeonError.None => Ok(result),
            DungeonError.DungeonNotFound => NotFound(new { message = "존재하지 않는 던전입니다." }),
            DungeonError.RunNotFound => NotFound(new { message = "던전 집입 기록을 찾지 못했습니다." }),
            DungeonError.NotYourRun => Forbid(), //403은 굳이 메시지 안줘도 된다.
            DungeonError.AlreadyRun => Conflict(new { message = "정산완료된 던전입니다." }),
            _ => StatusCode(500) //서버 에러
        };
    }

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out int userId) ? userId : null;
    }
}