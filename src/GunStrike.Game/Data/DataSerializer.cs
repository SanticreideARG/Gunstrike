using System.Text.Json;
using System.Text.Json.Serialization;

namespace GunStrike.Data;

/// <summary>
/// Save / load helpers for all editor data models.
/// All files are pretty-printed JSON with enum names (not ints).
/// </summary>
public static class DataSerializer
{
    // ── JSON options ─────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented         = true,
        PropertyNameCaseInsensitive = true,
        Converters            = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Maps ─────────────────────────────────────────────────────────────────

    public static void SaveMap(MapData map, string filePath)
    {
        EnsureDir(filePath);
        File.WriteAllText(filePath, JsonSerializer.Serialize(map, _opts));
    }

    public static MapData? LoadMap(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return JsonSerializer.Deserialize<MapData>(File.ReadAllText(filePath), _opts);
    }

    public static List<MapData> LoadAllMaps(string dir)
        => LoadAll<MapData>(dir, "*.map.json");

    // ── Weapons ───────────────────────────────────────────────────────────────

    public static void SaveWeapon(WeaponData weapon, string filePath)
    {
        EnsureDir(filePath);
        File.WriteAllText(filePath, JsonSerializer.Serialize(weapon, _opts));
    }

    public static WeaponData? LoadWeapon(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return JsonSerializer.Deserialize<WeaponData>(File.ReadAllText(filePath), _opts);
    }

    public static List<WeaponData> LoadAllWeapons(string dir)
        => LoadAll<WeaponData>(dir, "*.weapon.json");

    // ── Enemies ───────────────────────────────────────────────────────────────

    public static void SaveEnemy(EnemyData enemy, string filePath)
    {
        EnsureDir(filePath);
        File.WriteAllText(filePath, JsonSerializer.Serialize(enemy, _opts));
    }

    public static EnemyData? LoadEnemy(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return JsonSerializer.Deserialize<EnemyData>(File.ReadAllText(filePath), _opts);
    }

    public static List<EnemyData> LoadAllEnemies(string dir)
        => LoadAll<EnemyData>(dir, "*.enemy.json");

    // ── Defaults (seeded data for first launch) ───────────────────────────────

    public static MapData    DefaultMap()    => new() { Name = "Level 1", Environment = MapEnvironment.Urban };
    public static WeaponData DefaultWeapon() => new() { Name = "M4A1",    Class = WeaponClass.Rifle, FireMode = FireMode.FullAuto };
    public static EnemyData  DefaultEnemy()  => new() { Name = "Grunt",   Class = EnemyClass.Grunt };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnsureDir(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    private static List<T> LoadAll<T>(string dir, string pattern)
    {
        if (!Directory.Exists(dir)) return [];
        var list = new List<T>();
        foreach (var f in Directory.EnumerateFiles(dir, pattern))
        {
            try
            {
                var obj = JsonSerializer.Deserialize<T>(File.ReadAllText(f), _opts);
                if (obj != null) list.Add(obj);
            }
            catch { /* skip malformed files */ }
        }
        return list;
    }
}
