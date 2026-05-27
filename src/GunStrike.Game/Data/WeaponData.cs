namespace GunStrike.Data;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum WeaponClass  { Pistol, Rifle, Shotgun, Sniper, RocketLauncher, Grenade }
public enum FireMode     { SemiAuto, FullAuto, BurstFire }
public enum ProjectileShape { Circle, Capsule }

// ── Projectile config ─────────────────────────────────────────────────────────

public class ProjectileConfig
{
    /// <summary>Muzzle velocity in meters/second.</summary>
    public float Speed         { get; set; } = 60f;
    /// <summary>Gravity multiplier (0 = no drop, 1 = full gravity).</summary>
    public float GravityScale  { get; set; } = 0.25f;
    /// <summary>Max range in meters before despawn.</summary>
    public float MaxRange      { get; set; } = 30f;
    /// <summary>Lifespan in seconds.</summary>
    public float Lifespan      { get; set; } = 3f;
    /// <summary>Damage on hit.</summary>
    public float Damage        { get; set; } = 25f;
    /// <summary>Impact force applied to ragdoll bodies.</summary>
    public float ImpactForce   { get; set; } = 8f;
    /// <summary>Radius of hit circle in meters.</summary>
    public float Radius        { get; set; } = 0.03f;
    /// <summary>Trail length in pixels.</summary>
    public int   TrailLength   { get; set; } = 12;
    public ProjectileShape Shape { get; set; } = ProjectileShape.Circle;
    /// <summary>Whether projectile passes through multiple targets.</summary>
    public bool  Penetrating   { get; set; } = false;
    /// <summary>Explosion radius in meters (0 = no explosion).</summary>
    public float ExplosionRadius { get; set; } = 0f;
}

// ── Weapon ────────────────────────────────────────────────────────────────────

public class WeaponData
{
    public string      Id            { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string      Name          { get; set; } = "New Weapon";
    public WeaponClass Class         { get; set; } = WeaponClass.Rifle;
    public FireMode    FireMode      { get; set; } = FireMode.SemiAuto;

    // ── Shooting ─────────────────────────────────────────────────────────────
    /// <summary>Rounds per second.</summary>
    public float FireRate       { get; set; } = 8f;
    /// <summary>Pellets per shot (>1 for shotgun spread).</summary>
    public int   PelletsPerShot { get; set; } = 1;
    /// <summary>Cone spread in degrees.</summary>
    public float Spread         { get; set; } = 0f;
    /// <summary>Burst size (only for BurstFire mode).</summary>
    public int   BurstCount     { get; set; } = 3;
    /// <summary>Delay between burst shots in seconds.</summary>
    public float BurstDelay     { get; set; } = 0.05f;

    // ── Ammo ─────────────────────────────────────────────────────────────────
    public int   MagSize        { get; set; } = 30;
    public int   ReserveAmmo    { get; set; } = 120;
    /// <summary>Reload time in seconds.</summary>
    public float ReloadTime     { get; set; } = 2.0f;

    // ── Recoil ────────────────────────────────────────────────────────────────
    /// <summary>Upward kick per shot in degrees.</summary>
    public float RecoilUp       { get; set; } = 2f;
    /// <summary>Horizontal drift per shot in degrees.</summary>
    public float RecoilSide     { get; set; } = 0.5f;
    /// <summary>Recoil recovery speed (degrees/s).</summary>
    public float RecoilRecovery { get; set; } = 12f;

    // ── Projectile ────────────────────────────────────────────────────────────
    public ProjectileConfig Projectile { get; set; } = new();

    // ── Display ───────────────────────────────────────────────────────────────
    /// <summary>Sprite/icon index in the weapon sheet.</summary>
    public int SpriteIndex { get; set; } = 0;
    public string Description { get; set; } = "";
}
