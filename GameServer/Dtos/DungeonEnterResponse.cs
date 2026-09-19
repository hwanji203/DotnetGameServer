using GameServer.Services;

namespace GameServer.DTOs;

public class DungeonEnterResponse
{
    public class SpawnInfo
    {
        public int MonsterId { get; set; }
        public string MonsterName { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public int Exp { get; set; }
        public int Gold { get; set; }
    }
    
    public int RunId { get; set; }
    public int DungeonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SpawnInfo> Spawns { get; set; } = new();

    public static DungeonEnterResponse From(int runId, DungeonDef dungeon, GameCatalog catalog)
    {
        List<SpawnInfo> spawns = new List<SpawnInfo>();

        foreach (DungeonSpawn spawn in dungeon.Spawns)
        {
            MonsterDef? monster = catalog.FindMonster(spawn.MonsterId);

            if (monster is null) continue; //한번 더 안전코드 검사.
            spawns.Add(new SpawnInfo()
            {
                MonsterId = spawn.MonsterId,
                MonsterName = monster.Name,
                Count = spawn.Count,
                Exp = monster.Exp,
                Gold = monster.Gold,
            });
        }
        
        return new DungeonEnterResponse()
        {
            RunId = runId,
            DungeonId = dungeon.Id,
            Name = dungeon.Name,
            Spawns = spawns
        };
    }
}