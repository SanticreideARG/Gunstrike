using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using GunStrike.Core;
using GunStrike.Physics;

namespace GunStrike.Entities;

/// <summary>
/// Manages all active projectiles.
/// Handles spawning, lifecycle, impact resolution, and muzzle flash.
/// </summary>
public class ProjectileManager
{
    private readonly PhysicsWorld   _pw;
    private readonly PlayerEntity   _player;
    private readonly List<Projectile> _active = [];

    // Muzzle flash state
    private float _flashTimer;
    private Vector2 _flashPosPx;
    private const float FlashDuration = 0.06f;   // seconds

    // Fire-rate limiter
    private float _cooldown;
    private const float FireRate = 0.12f;   // seconds between shots

    /// <summary>Fired when a bullet hits an enemy body. Args: hitBody, hitPointMeters, damage.</summary>
    public event Action<Body, Vector2, float>? OnEnemyHit;

    public int ActiveCount => _active.Count;

    public ProjectileManager(PhysicsWorld pw, PlayerEntity player)
    {
        _pw     = pw;
        _player = player;
    }

    // ── Shoot ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn a bullet from originMeters in the given direction.
    /// originMeters should be in the physics world (meters).
    /// </summary>
    public void Shoot(Vector2 originMeters, Vector2 directionNorm)
    {
        if (_cooldown > 0f) return;

        var velocity = directionNorm * GameConstants.BulletSpeed;
        _active.Add(new Projectile(_pw, originMeters, velocity));

        _cooldown = FireRate;

        // Muzzle flash (pixel position, for HUD overlay — drawn outside camera)
        _flashTimer = FlashDuration;
        _flashPosPx = originMeters * GameConstants.PhysicsToPixels;
    }

    // ── Update ───────────────────────────────────────────────────────────────────

    public void Update(float dt)
    {
        _cooldown  = Math.Max(0f, _cooldown - dt);
        _flashTimer = Math.Max(0f, _flashTimer - dt);

        // Update all projectiles
        foreach (var p in _active) p.Update(dt);

        // Resolve impacts then remove dead ones
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var p = _active[i];
            if (!p.IsDead) continue;

            if (p.HitPlayer)
                ResolvePlayerHit(p);

            if (p.HitEnemy && p.HitEnemyBody is not null)
                OnEnemyHit?.Invoke(p.HitEnemyBody, p.HitPointMeters, p.Damage);

            p.Destroy(_pw);
            _active.RemoveAt(i);
        }
    }

    private void ResolvePlayerHit(Projectile p)
    {
        // Impulse = bullet_speed * bullet_mass * amplifier
        const float ImpactAmplifier = 8f;
        var impulseMps = Vector2.Normalize(
            new Vector2(p.Body.LinearVelocity.X == 0
                ? (p.HitPointMeters.X - _player.TorsoMeters.X)
                : p.Body.LinearVelocity.X,
                        p.Body.LinearVelocity.Y == 0
                ? (p.HitPointMeters.Y - _player.TorsoMeters.Y)
                : p.Body.LinearVelocity.Y))
            * GameConstants.BulletSpeed * 0.01f * ImpactAmplifier;

        _player.ApplyImpact(p.HitPointMeters, impulseMps);
    }

    // ── Draw (inside BeginMode2D) ────────────────────────────────────────────────

    public void Draw()
    {
        foreach (var p in _active) p.Draw();
    }

    // ── Muzzle flash (screen space, outside BeginMode2D) ────────────────────────
    // cameraWorldX/Y: camera target world pixel position
    public void DrawMuzzleFlash(Vector2 cameraTargetPx, Vector2 screenOffset)
    {
        if (_flashTimer <= 0f) return;

        float alpha = _flashTimer / FlashDuration;
        var screenPos = _flashPosPx - cameraTargetPx + screenOffset;

        float r = 28f * alpha;
        Raylib.DrawCircleV(screenPos, r,
            new Color(255, 220, 80, (int)(200 * alpha)));
        Raylib.DrawCircleV(screenPos, r * 0.5f,
            new Color(255, 255, 200, (int)(255 * alpha)));
    }
}
