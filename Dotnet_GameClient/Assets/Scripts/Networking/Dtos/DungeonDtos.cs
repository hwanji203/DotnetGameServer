using System.Collections.Generic;

namespace Networking.Dtos
{
    /// <summary>
    /// 던전에 등장하는 몬스터 한 종류 데이터(수와 단가)
    /// </summary>
    public class SpawnInfo
    {
        public int MonsterId { get; set; }
        public string MonsterName { get; set; }
        public int Count { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
    }

    public class DungeonEnterResponse
    {
        public int RunId { get; set; }
        public int DungeonId { get; set; }
        public string Name { get; set; }
        public List<SpawnInfo> Spawns { get; set; } = new();
    }

    public class MonsterKill
    {
        public int MonsterId { get; set; }
        public int Count { get; set; }
    }

    public class DungeonCompleteRequest
    {
        public List<MonsterKill> Kills { get; set; } = new();
    }

    //서버가 보내주는 정산 결과
    public class DungeonResultResponse
    {
        public int DungeonId { get; set; }
        public int MonsterKilled { get; set; }
        public long GoldGained { get; set; }
        public long ExpGained { get; set; }
        public long TotalGold { get; set; }
        public CharacterResponse Character { get; set; }
    }
}