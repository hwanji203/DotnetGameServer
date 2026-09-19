namespace GameServer.Services;

public class MonsterDef
{
    public int Id {get; set;}
    public string Name { get; set; } = "";
    public int Exp {get; set;}
    public int Gold {get; set;}
}

public class DungeonSpawn
{
    public int MonsterId {get; set;}
    public int Count {get; set;}
}

public class DungeonDef
{
    public int Id {get; set;}
    public string Name { get; set; } = "";
    public int ClearGoldBonus {get; set;}
    public List<DungeonSpawn> Spawns {get; set;} = new List<DungeonSpawn>();
}