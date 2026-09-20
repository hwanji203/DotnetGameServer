using GameServer.Data;
using GameServer.DTOs;
using GameServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Services;

public enum GuildError
{
    None = 0,
    AlreadyInGuild = 1,  //이미 길드 있음.
    NameTaken = 2,      //이름이 중복
    GuildNotFound = 3,  //해당 길드 없음.
    GuildFull = 4,      //길드 인원 꽉 참.
    // ApprovalRequired = 5,       //승인제 길드 (즉시 가입 불가능함
    NotInGuild = 5,         //소속길드 없음.
    NotMaster,          //마스터가 아닌데 마스터 기능을 쓰려고 할 때 발생
    AlreadyRequested,   //이미 대기중인 가입신청이 존재해.
    RequestNotFound,    //그런 신청이 없어. 없는 신청에 대한 작업시 발생
    RequestNotPending,  //이미 처리된 신청
    ApplicantAlreadyInGuild,    // 승인하려고 하는데, 그 신청자가 이미 다른 길드에 가입
    TargetNotMember, //양도 또는 강퇴 시키려는 멤버가 우리 길드가 아님
    CannotTargetSelf,   //스스로 자기에게 양도 또는 강퇴처리 불가능
}

public class GuildService
{
    private readonly AppDbContext _db;

    public GuildService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(GuildError error, GuildResponse? guildRes)> CreateAsync(int userId, string name,
        GuildJoinMode joinMode)
    {
        //개설하려는 유저가 이미 길드에 신청 또는 가입중
        if(await _db.GuildMembers.AnyAsync(member => member.UserId == userId))
            return (GuildError.AlreadyInGuild, null);
        
        //개설하려는 길드의 이름이 이미 존재함.
        if(await _db.Guilds.AnyAsync(g => g.Name == name))
            return (GuildError.NameTaken, null);
        
        Guild newGuild = new Guild {Name = name, JoinMode = joinMode};
        //길드장으로 가입시켜둔다.
        newGuild.Members.Add(new GuildMember{UserId = userId, Role = GuildRole.Master});
        _db.Guilds.Add(newGuild);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            return (GuildError.NameTaken, null);
        }

        return (GuildError.None, await LoadResponseAsync(newGuild.Id));
    }

    public async Task<(GuildError error, GuildJoinResponse? guildRes)> JoinAsync(int userId, int guildId)
    {
        if(await _db.GuildMembers.AnyAsync(m => m.UserId == userId))
            return (GuildError.AlreadyInGuild, null); //해당 유저가 이미 다른 길드 또는 이 길드에 가입중
        
        Guild? targetGuild = await _db.Guilds
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == guildId);
        
        if(targetGuild is null)
            return (GuildError.GuildNotFound, null);
        if(targetGuild.Members.Count >= GuildRules.BaseCapacity)
            return (GuildError.GuildFull, null);

        if (targetGuild.JoinMode == GuildJoinMode.Approval)
        {
            bool hasPending =
                await _db.GuildJoinRequests.AnyAsync(r =>
                    r.UserId == userId && r.Status == GuildJoinRequestStatus.Pending);
            
            //이미 다른 길드에 신청중인 유저라면.
            if (hasPending)
                return (GuildError.AlreadyRequested, null);

            GuildJoinRequest request = new GuildJoinRequest { GuildId = guildId, UserId = userId };
            _db.GuildJoinRequests.Add(request);
            await _db.SaveChangesAsync();
            return (GuildError.None, GuildJoinResponse.Pending(request.Id));
        }
        
        //길드 ID는 넣지 않아도 이미 targetGuild 의 멤버이기 때문에
        targetGuild.Members.Add(new GuildMember{UserId = userId, Role = GuildRole.Member});
        await CancelPendingRequestAsync(userId); //해당 유저로 가입한 대기신청들을 전부 없애고
        await _db.SaveChangesAsync();

        GuildResponse? joinedGuild = await LoadResponseAsync(targetGuild.Id);
        
        return (GuildError.None, GuildJoinResponse.Joined(joinedGuild!));
    }

    public async Task CancelPendingRequestAsync(int userId)
    {
        List<GuildJoinRequest> pendingList = await _db.GuildJoinRequests
            .Where(r => r.UserId == userId && r.Status == GuildJoinRequestStatus.Pending)
            .ToListAsync();

        foreach (GuildJoinRequest pending in pendingList)
        {
            Decide(pending, GuildJoinRequestStatus.Canceled);
        }
    }

    private void Decide(GuildJoinRequest request, GuildJoinRequestStatus status)
    {
        request.Status = status;
        request.DecideAt = DateTime.UtcNow;
    }

    public async Task<GuildError> LeaveAsync(int userId)
    {
        GuildMember? me = await _db.GuildMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        
        if(me is null)
            return GuildError.NotInGuild;

        if (me.Role == GuildRole.Master)
        {
            //같은 길드의 유저들 중에서 가입일이 가장 빠른 유저를 가져오는 쿼리
            GuildMember? successor = await _db.GuildMembers
                .Where(m => m.GuildId == me.GuildId && m.UserId != userId)
                .OrderBy(m => m.JoinedAt)
                .FirstOrDefaultAsync();

            if (successor is null)
            {
                //길드에 자기밖에 없었던 것
                Guild guild = await _db.Guilds.FirstAsync(g => g.Id == me.GuildId);
                _db.Guilds.Remove(guild);
                await _db.SaveChangesAsync();
                return GuildError.None;
            }
            
            successor.Role = GuildRole.Master; //계승자를 길드장으로 올라온다. 
        }
        
        _db.GuildMembers.Remove(me);//일반회원이면 그냥 리무브도 괜찮아.
        await _db.SaveChangesAsync();
        return GuildError.None;
    }
    
    //자기 길드의 정보를 조회
    public async Task<GuildResponse?> GetMyGuildAsync(int userId)
    {
        GuildMember? me = await _db.GuildMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        return me is null ? null : await LoadResponseAsync(me.GuildId);
    }
    
    //길드 목록 조회
    public async Task<List<GuildListItem>> ListAsync()
    {
        //익명 형태라 List 를 var로 선언함.
        var rows = await _db.Guilds
            .Select(g => new {g.Id, g.Name, g.JoinMode, g.Level, MemberCount = g.Members.Count})
            .ToListAsync();

        return rows.Select(r => new GuildListItem
        {
            Id = r.Id,
            Name = r.Name,
            JoinMode = r.JoinMode.ToString(),
            Level = r.Level,
            MemberCount = r.MemberCount,
            Capacity = GuildRules.BaseCapacity
        }).ToList();
    }
    
    private async Task<GuildResponse?> LoadResponseAsync(int newGuildId)
    {
        var guild = await _db.Guilds
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == newGuildId);
        return guild is null ? null : GuildResponse.From(guild);
    }
    
    //내 길드에 대기중인 가입신청 몰곩을 가져우는것. 길드장만 볼 수 있따.
    public async Task<(GuildError error, List<GuildJoinRequestItem>? items)> ListRequestAsync(int userId)
    {
        (GuildError error, GuildMember? me) = await RequireMasterAsync(userId);
        if (error != GuildError.None)
            return (error, null); //에러가 존재한다면 이 작업은 처리하면 안되니 리ㅓㄴ
        
        //내 길드에 대기중인 요청을 GuildJoinitem 형태로 변경하여 가져온다.
        List<GuildJoinRequestItem> items = await _db.GuildJoinRequests
            .Where(r => r.GuildId == me!.GuildId && r.Status ==  GuildJoinRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .Select(r => new GuildJoinRequestItem
            {
                Id = r.Id,
                UserId = r.UserId,
                NickName = r.User.Nickname,
                RequestedAt =r.RequestedAt
            }).ToListAsync();
        
        return (GuildError.None, items);
    }
    
    private async Task<(GuildError GuildError, GuildMember? me)> RequireMasterAsync(int userId)
    {
        GuildMember? me = await _db.GuildMembers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (me is null)
            return (GuildError.NotInGuild, null); //길드에 속해있지 않음
        if (me.Role != GuildRole.Master)
            return (GuildError.NotMaster, null); //길드장이 아님

        return (GuildError.None, me); //성공적으로 조회시
    }

    public async Task<(GuildError error, GuildResponse? guild)> ApproveRequestAsync(int userId, int requestId)
    {
        (GuildError error, GuildMember? me) = await RequireMasterAsync(userId);
        if (error != GuildError.None)
            return (error, null);

        GuildJoinRequest? request = await _db.GuildJoinRequests.FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null || request.GuildId != me!.GuildId)
            return (GuildError.RequestNotFound, null); //남의 길드거나 없는 경우
        if (request.Status != GuildJoinRequestStatus.Pending)
            return (GuildError.RequestNotFound, null); // 이미 요청의 처리가 끝난 경우
        
        //만약 승인을 하려는 순간 사용자가 다른 길드에 이미 가입되어 있다면 신청을 무효처리한(어지간해서는 발생하지 않는다)
        if (await _db.GuildMembers.AnyAsync(m => m.UserId == request.UserId))
        {
            Decide(request, GuildJoinRequestStatus.Canceled);
            await _db.SaveChangesAsync();
            return (GuildError.ApplicantAlreadyInGuild, null); //이미 다른 길드에 가입되었음.
        }
        
        //이제부터 승인처리 시작
        int memberCount = await _db.GuildMembers.CountAsync(m => m.GuildId == me.GuildId);
        if (memberCount >= GuildRules.BaseCapacity)
            return (GuildError.GuildFull, null); //길드청원 초과(차후 어빌리티 퍽으로 추가되면 여기 변경되어야 해)

        _db.GuildMembers.Add(new GuildMember
        {
            GuildId = me.GuildId,
            UserId = request.UserId,
            Role = GuildRole.Member
        });
        
        Decide(request, GuildJoinRequestStatus.Approved); //승인으로 돌리기
        await _db.SaveChangesAsync();
        
        return (GuildError.None, await LoadResponseAsync(me.GuildId));
    }
    
    //길드 요청 거절 (길드장만 가능)
    public async Task<GuildError> RejectRequestAsync(int userId, int requestId)
    {
        (GuildError error, GuildMember? me) = await RequireMasterAsync(userId);
        if (error != GuildError.None)
            return error;

        GuildJoinRequest? request = await _db.GuildJoinRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null || request.GuildId != me!.GuildId)
            return GuildError.RequestNotFound;
        if (request.Status != GuildJoinRequestStatus.Pending)
            return GuildError.RequestNotPending;
        
        Decide(request, GuildJoinRequestStatus.Rejected);
        await _db.SaveChangesAsync();
        return GuildError.None;
    }

    //길드원 강제 퇴장시키기
    public async Task<GuildError> CancelMyRequestAsync(int userId)
    {
        GuildJoinRequest? request = await _db.GuildJoinRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == GuildJoinRequestStatus.Pending);

        if (request is null)
            return GuildError.RequestNotFound;
        
        Decide(request, GuildJoinRequestStatus.Canceled);
        await _db.SaveChangesAsync();
        return GuildError.None;
    }
    
    //길드장 양도
    public async Task<(GuildError error, GuildResponse? guild)> KickAsync(int userId, int targetUserId)
    {
        (GuildError error, GuildMember? me) = await RequireMasterAsync(userId);
        if (error != GuildError.None)
            return (error, null);
        if (targetUserId == userId)
            return (GuildError.CannotTargetSelf, null); //자기자신 추방은 안돼.(그건 탈퇴)

        //길마와 같은 길드에 있는지 체크
        GuildMember? target = await _db.GuildMembers
            .FirstOrDefaultAsync(m => m.UserId == targetUserId && m.GuildId == me!.GuildId);
        if (target is null)
            return (GuildError.TargetNotMember, null);

        _db.GuildMembers.Remove(target);
        await _db.SaveChangesAsync();

        return (GuildError.None, await LoadResponseAsync(me!.GuildId));
    }
    
    public async Task<(GuildError error, GuildResponse? guild)> TransferMasterAsync(int userId, int targetUserId)
    {
        (GuildError error, GuildMember? me) = await RequireMasterAsync(userId);
        if (error != GuildError.None)
            return (error, null);
        if (targetUserId == userId)
            return (GuildError.CannotTargetSelf, null); //자신은 이미 길드장.

        GuildMember? target = await _db.GuildMembers
            .FirstOrDefaultAsync(m => m.UserId == targetUserId && m.GuildId == me!.GuildId);
        if (target is null)
            return (GuildError.TargetNotMember, null);

        target.Role = GuildRole.Master;
        me!.Role = GuildRole.Member;
        await _db.SaveChangesAsync();

        return (GuildError.None, await LoadResponseAsync(me.GuildId));
    }
}