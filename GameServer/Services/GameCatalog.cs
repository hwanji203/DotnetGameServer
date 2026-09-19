using System.Text.Json;

namespace GameServer.Services;

/// <summary>
/// 기획으로 작성한 파일 데이터를 서버 부팅시에 Json에서 읽어들여서 한번에 메모리로 상주시키는 카탈로그
/// </summary>
public class GameCatalog
{
    private readonly IReadOnlyDictionary<int, MonsterDef> _monsteres;
    private readonly IReadOnlyDictionary<int, DungeonDef> _dungeons;
    
    //외부에서 이를 주입한다.
    private GameCatalog(IReadOnlyDictionary<int, MonsterDef> monsters,
        IReadOnlyDictionary<int, DungeonDef> dungeons)
    {
        _monsteres = monsters;
        _dungeons = dungeons;
    }

    public MonsterDef? FindMonster(int id) => _monsteres.GetValueOrDefault(id);
    public DungeonDef? FindDungeon(int id) => _dungeons.GetValueOrDefault(id);
    
    //Json은 기본적으로 Camelcase이나 C#은 PascalCase이다(앞문자 대분자) 따라서 JsonSerialize을 셋팅 필요
    //Web옵션은 대소문자를 무시하는 옵션이다
    private static readonly JsonSerializerOptions JsonOptions
        = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    
    //실제 로드 함수
    public static GameCatalog LoadFrom(string contentRoot)
    {
        string dir = Path.Combine(contentRoot, "GameData"); //루트폴더에 있는 GameData 폴더를 읽는다.

        Dictionary<int, MonsterDef> monsters = ReadJson<MonsterDef>(Path.Combine(dir, "monsters.json"))
            .ToDictionary(m => m.Id);
        Dictionary<int, DungeonDef> dungeons = ReadJson<DungeonDef>(Path.Combine(dir, "dungeons.json"))
            .ToDictionary(m => m.Id);
        //데이터 무결성 검사가 이뤄져야 한다.

        foreach (DungeonDef dungeon in dungeons.Values)
            foreach(DungeonSpawn spawn in dungeon.Spawns)
                if (!monsters.ContainsKey(spawn.MonsterId))
                    throw new InvalidOperationException(
                        $"[던전 {dungeon.Name}] {dungeon.Id}가 참조하는 몬스터 {spawn.MonsterId} 가 정의되지 않았습니다.");
        
        return new GameCatalog(monsters, dungeons);
    }

    private static List<T> ReadJson<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"마스터 데이터 파일을 찾을 수 없습니다. :{filePath} ");
        
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"마스터 데이터 파일 파싱 실패 : {filePath}");
        //데이터 무결성 검사가 이뤄져야 한다.
    }
}