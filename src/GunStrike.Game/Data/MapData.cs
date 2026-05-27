using System.Text.Json.Serialization;

namespace GunStrike.Data;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum MapEnvironment { Mountains, Dunes, Urban }
public enum TileType       { Solid, Platform, Ladder, Spike, Water }

// ── Tile ──────────────────────────────────────────────────────────────────────

public class TileData
{
    /// <summary>Grid column (tile units).</summary>
    public int X { get; set; }
    /// <summary>Grid row (tile units).</summary>
    public int Y { get; set; }
    /// <summary>Tile sprite/variant index.</summary>
    public int SpriteIndex { get; set; }
    public TileType Type { get; set; } = TileType.Solid;
}

// ── Spawn point ───────────────────────────────────────────────────────────────

public class SpawnPoint
{
    public string Name { get; set; } = "Player";
    /// <summary>World position in pixels.</summary>
    public float X { get; set; }
    public float Y { get; set; }
}

// ── Enemy instance placed on map ──────────────────────────────────────────────

public class EnemyInstance
{
    /// <summary>Refers to EnemyData.Id.</summary>
    public string EnemyId  { get; set; } = "";
    public float  X        { get; set; }
    public float  Y        { get; set; }
    /// <summary>Patrol range in pixels from spawn X.</summary>
    public float  PatrolRange { get; set; } = 200f;
}

// ── Map ───────────────────────────────────────────────────────────────────────

public class MapData
{
    public string         Id          { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string         Name        { get; set; } = "New Map";
    public MapEnvironment Environment { get; set; } = MapEnvironment.Mountains;

    /// <summary>Map width in tile units.</summary>
    public int  WidthTiles  { get; set; } = 80;
    /// <summary>Map height in tile units.</summary>
    public int  HeightTiles { get; set; } = 20;
    /// <summary>Pixel size of one tile.</summary>
    public int  TileSize    { get; set; } = 32;

    public List<TileData>      Tiles   { get; set; } = [];
    public List<SpawnPoint>    Spawns  { get; set; } = [new SpawnPoint { Name = "Player", X = 160f, Y = 400f }];
    public List<EnemyInstance> Enemies { get; set; } = [];

    // ── Derived helpers ──────────────────────────────────────────────────────
    [JsonIgnore] public float WidthPixels  => WidthTiles  * TileSize;
    [JsonIgnore] public float HeightPixels => HeightTiles * TileSize;
}
