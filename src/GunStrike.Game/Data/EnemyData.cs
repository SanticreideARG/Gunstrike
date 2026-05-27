namespace GunStrike.Data;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum EnemyClass      { Grunt, Heavy, Sniper, Grenadier, Boss }
public enum AIBehavior      { Patrol, Guard, Aggressive, Coward }
public enum AlertTrigger    { LineOfSight, Proximity, Always }

// ── AI config ─────────────────────────────────────────────────────────────────

public class AIConfig
{
    public AIBehavior    Behavior          { get; set; } = AIBehavior.Patrol;
    public AlertTrigger  AlertTrigger      { get; set; } = AlertTrigger.LineOfSight;

    /// <summary>Sight range in meters.</summary>
    public float SightRange      { get; set; } = 12f;
    /// <summary>Hearing range in meters (triggered by gunshots etc).</summary>
    public float HearRange       { get; set; } = 8f;
    /// <summary>Reaction delay in seconds after spotting player.</summary>
    public float ReactionTime    { get; set; } = 0.4f;
    /// <summary>Walking speed in meters/second.</summary>
    public float WalkSpeed       { get; set; } = 2.5f;
    /// <summary>Running speed in meters/second.</summary>
    public float RunSpeed        { get; set; } = 5.0f;
    /// <summary>Preferred attack range in meters.</summary>
    public float AttackRange     { get; set; } = 8f;
    /// <summary>Time between shots in seconds.</summary>
    public float AttackInterval  { get; set; } = 1.2f;
    /// <summary>Accuracy: 0 = perfect, 1 = terrible (spread multiplier).</summary>
    public float Inaccuracy      { get; set; } = 0.3f;
    /// <summary>Chance to seek cover when hit (0..1).</summary>
    public float SeekCoverChance { get; set; } = 0.5f;
}

// ── Stats ─────────────────────────────────────────────────────────────────────

public class EnemyStats
{
    public float MaxHealth      { get; set; } = 100f;
    public float Armor          { get; set; } = 0f;
    /// <summary>Ragdoll mass multiplier vs default.</summary>
    public float MassMult       { get; set; } = 1f;
    /// <summary>Score awarded on kill.</summary>
    public int   ScoreValue     { get; set; } = 100;
    /// <summary>Chance to drop pickup (0..1).</summary>
    public float DropChance     { get; set; } = 0.2f;
}

// ── Enemy ─────────────────────────────────────────────────────────────────────

public class EnemyData
{
    public string      Id          { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string      Name        { get; set; } = "New Enemy";
    public EnemyClass  Class       { get; set; } = EnemyClass.Grunt;
    public string      Description { get; set; } = "";

    public EnemyStats  Stats       { get; set; } = new();
    public AIConfig    AI          { get; set; } = new();

    /// <summary>Weapon IDs this enemy can carry (random pick at spawn).</summary>
    public List<string> WeaponPool { get; set; } = [];

    /// <summary>Sprite sheet row index.</summary>
    public int SpriteRow           { get; set; } = 0;

    // ── Quick-read helpers ────────────────────────────────────────────────────
    /// <summary>Human-readable class label.</summary>
    public string ClassLabel => Class switch
    {
        EnemyClass.Grunt      => "Grunt",
        EnemyClass.Heavy      => "Heavy",
        EnemyClass.Sniper     => "Sniper",
        EnemyClass.Grenadier  => "Grenadier",
        EnemyClass.Boss       => "BOSS",
        _                     => "?"
    };
}
