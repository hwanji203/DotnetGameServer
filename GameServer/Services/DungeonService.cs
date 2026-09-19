using GameServer.Data;
using GameServer.DTOs;
using GameServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Services;

public enum DungeonError
{
    None = 0,
    DungeonNotFound = 1,
    RunNotFound = 2,
    NotYourRun = 3, //남의 런으로 천산을 시도하려는 클라의 시도를 차단
    AlreadyRun = 4, //이미 정산완료된  런으로 또 정산받으려 할 때 차단
}
public class DungeonService
{
    private readonly AppDbContext _db;
    private readonly CharacterService _characterService;
    private readonly GameCatalog _catalog;
    
    //의존성 주입
    public DungeonService(GameCatalog catalog, AppDbContext db, CharacterService characterService)
    {
        _catalog = catalog;
        _db = db;
        _characterService = characterService;
    }

    public async Task<DungeonEnterResponse?> EnterDungeonAsync(int userId, int dungeonId)
    {
        DungeonDef? dungeon = _catalog.FindDungeon(dungeonId);
        if (dungeon is null)
            return null;

        DungeonRun run = new DungeonRun { UserId = userId, DungeonId = dungeonId };
        _db.DungeonRuns.Add(run);
        await _db.SaveChangesAsync();

        return DungeonEnterResponse.From(run.Id, dungeon, _catalog);
    }
    
    //던전 결과 제출 서비스
    public async Task<(DungeonError error, DungeonResultResponse? result)> CompleteAsync(int userid, int runId,
        DungeonCompleteRequests request)
    {
        DungeonRun? run = await _db.DungeonRuns.FirstOrDefaultAsync(run => run.Id == runId);

        if (run is null)
            return (DungeonError.RunNotFound, null);
        if (run.UserId != userid)
            return (DungeonError.NotYourRun, null);
        if (run.Status != DungeonRunStatus.InProgress)
            return (DungeonError.AlreadyRun, null);

        DungeonDef? dungeon = _catalog.FindDungeon(run.DungeonId); //해당 던전 정보를 알아온다.
        
        //최대 처치 가능한 몬스터수 캡
        Dictionary<int, int> spawnCap = dungeon.Spawns
            .ToDictionary(s => s.MonsterId, s => s.Count);

        Dictionary<int, int> claims = request.Kills
            .GroupBy(k => k.MonsterId)
            .ToDictionary(g => g.Key, g => g.Sum(k => k.Count));

        int totalKilled = 0;
        long goldGained = 0;
        long expGained = 0;

        foreach ((int monsterId, int claimedCount) in claims)
        {
            int cap = spawnCap.GetValueOrDefault(monsterId, 0);
            MonsterDef? monster = _catalog.FindMonster(monsterId);
            if (monster is null || cap == 0)
                continue; //해당 몬스터는 청산 무시

            int killed = Math.Clamp(claimedCount, 0, cap);
            //cap안에섬나 인접함. 추가 데이터는 인정하지 않음. 나중에 여기에 로그를 기록해서 남기면 차단 검출도 가능함.
            totalKilled += killed;
            goldGained += (long)killed * monster.Gold;
            expGained += (long)killed * monster.Exp;
        }

        goldGained += dungeon.ClearGoldBonus; //클리어 보너스 골드 지급

        User user = await _db.Users.FirstAsync(u => u.Id == userid);
        user.Gold += goldGained;

        run.Status = DungeonRunStatus.Completed;
        run.CompletedAt = DateTime.UtcNow;
        run.MonsterKilled = totalKilled;
        run.GoldGained = goldGained;
        run.ExpGained = expGained;
        
        //이제 경험치를 지급해서 플레이어가 렙업을 했다면 레벨업 처리를 해야함.

        CharacterResponse? character = await _characterService.AddExpAsync(userid, (int)expGained);

        var result = new DungeonResultResponse
        {
            DungeonId = run.DungeonId,
            MonsterKilled = totalKilled,
            GoldGained = goldGained,
            ExpGained = expGained,
            TotalGold = user.Gold,
            Character = character!
        };
        return (DungeonError.None, result);
    }
}