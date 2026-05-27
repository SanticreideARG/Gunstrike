using System.Numerics;
using nkast.Aether.Physics2D.Dynamics;
using GunStrike.Data;
using GunStrike.Physics;

namespace GunStrike.Entities;

/// <summary>Manages all active enemy entities.</summary>
public class EnemyManager
{
    private readonly PhysicsWorld                  _pw;
    private readonly List<EnemyEntity>             _enemies = [];
    private readonly Dictionary<Body, EnemyEntity> _bodyMap = [];

    /// <summary>Fired when any enemy deals hitscan damage to the player. Arg: damage.</summary>
    public event Action<float>? OnPlayerHit;

    public int AliveCount => _enemies.Count(e => !e.IsDead);
    public int TotalCount => _enemies.Count;

    public EnemyManager(PhysicsWorld pw) { _pw = pw; }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    public EnemyEntity Spawn(EnemyData data, Vector2 spawnMeters, float patrolRange = 4f)
    {
        var e = new EnemyEntity(_pw, data, spawnMeters, patrolRange);
        e.OnHitPlayer += dmg => OnPlayerHit?.Invoke(dmg);
        _enemies.Add(e);
        _bodyMap[e.PhysicsBody] = e;
        return e;
    }

    // ── Bullet hit (from ProjectileManager.OnEnemyHit) ───────────────────────

    public void ApplyBulletHit(Body body, Vector2 hitPoint, float damage)
    {
        if (_bodyMap.TryGetValue(body, out var enemy))
            enemy.TakeDamage(damage, hitPoint);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public void Update(float dt, Vector2 playerMeters, bool playerAlive)
    {
        foreach (var e in _enemies)
            e.Update(dt, playerMeters, playerAlive);

        // Remove fully-faded dead enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (_enemies[i].ShouldRemove)
            {
                _bodyMap.Remove(_enemies[i].PhysicsBody);
                _enemies[i].Destroy();
                _enemies.RemoveAt(i);
            }
        }
    }

    // ── Draw (inside BeginMode2D) ─────────────────────────────────────────────

    public void Draw()
    {
        foreach (var e in _enemies) e.Draw();
    }
}
