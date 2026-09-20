using GameServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    //엔티티로 만들어진 테이블을 선언한다. Linq 쿼리가 이걸로 시작한다.
    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<DungeonRun> DungeonRuns => Set<DungeonRun>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<GuildJoinRequest> GuildJoinRequests => Set<GuildJoinRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Character)
            .WithOne(c => c.User)
            .HasForeignKey<Character>(c => c.UserId); //이게 외래키다.

        modelBuilder.Entity<DungeonRun>()
            .HasIndex(run => new { run.UserId, run.Status }); //UserId, Status 인덱스 넣으면
        
        //길드 이름이 중복 불가능해야함.
        modelBuilder.Entity<Guild>()
            .HasIndex(g => g.Name)
            .IsUnique();
        
        //멤버 관계에서 userId를 유니크 설정해서 유저는 한개의 길드만을 강제 당해야해.
        modelBuilder.Entity<GuildMember>()
            .HasIndex(m => m.UserId)
            .IsUnique();
        
        //길드 삭제시 길드 멤버도 같이 삭제된다. (user가 삭제되는건 아님)
        modelBuilder.Entity<GuildMember>()
            .HasOne(m => m.Guild)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 이거는 1: 다 관계다를 이야기하는거야
        modelBuilder.Entity<GuildMember>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //길드 가입 신청과 길드 가입 및 유저 관계
        modelBuilder.Entity<GuildJoinRequest>()
            .HasOne(r => r.Guild)
            .WithMany()
            .HasForeignKey(r => r.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<GuildJoinRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        //길드장이 대기 목록 조회, 유저가 내 대기신청 조회를 하면 빠르게 해야하니까
        modelBuilder.Entity<GuildJoinRequest>()
            .HasIndex(r => new { r.GuildId, r.Status });
        modelBuilder.Entity<GuildJoinRequest>()
            .HasIndex(r => new { r.UserId, r.Status });
    }
}