using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using GunStrike.Core;
using GunStrike.Physics;

namespace GunStrike.Entities;

/// <summary>
/// A single ballistic projectile.
///
/// Physics:
///   - Fast kinematic body (avoids CCD tunnelling at 60 m/s).
///   - Position is integrated manually each frame so we can do
///     exact sub-step raycast-style checks.
///   - Gravity applies partial drag (GravityScale = 0.25 → slight arc).
///
/// Visual:
///   - Bright dot at head position.
///   - Fading trail of N positions.
/// </summary>
public class Projectile
{
    // ── Physics state ────────────────────────────────────────────────────────────
    public  Body    Body       { get; }
    private Vector2 _velMps;          // meters/s  (System.Numerics)

    // ── Lifecycle ────────────────────────────────────────────────────────────────
    private float _age;
    public  bool  IsDead       => _age >= GameConstants.BulletLifespan || _hitSomething;

    // ── Collision result ────────────────────────────────────────────────────────
    public  bool    HitPlayer      { get; private set; }
    public  Body?   HitPlayerBody  { get; private set; }
    public  Vector2 HitPointMeters { get; private set; }
    private bool    _hitSomething;

    // ── Rendering trail ──────────────────────────────────────────────────────────
    private const int TrailLen = 10;
    private readonly Queue<Vector2> _trail = new();   // pixel positions

    // ── Colors ───────────────────────────────────────────────────────────────────
    private static readonly Color HeadColor  = new(255, 240, 80,  255);
    private static readonly Color TrailStart = new(255, 200, 60,  200);
    private static readonly Color TrailEnd   = new(255, 140, 30,  0);

    // ── Constructor ──────────────────────────────────────────────────────────────
    public Projectile(PhysicsWorld pw, Vector2 originMeters, Vector2 velocityMps)
    {
        _velMps = velocityMps;

        // Small kinematic circle — we move it manually
        Body = pw.World.CreateBody(new AetherVec2(originMeters.X, originMeters.Y),
                                   0f, BodyType.Kinematic);
        var fix = Body.CreateCircle(0.04f, 1f, AetherVec2.Zero);
        fix.IsSensor = true;                   // sensor: detects overlap without physics response
        fix.CollisionCategories = PhysicsCategories.Bullet;
        fix.CollidesWith = PhysicsCategories.All;

        Body.OnCollision += OnCollision;
        Body.Tag = "bullet";
    }

    // ── Update ───────────────────────────────────────────────────────────────────
    public void Update(float dt)
    {
        if (IsDead) return;

        _age += dt;

        // Ballistic arc: apply gravity manually
        _velMps += new Vector2(0f, GameConstants.GravityY * 0.25f * dt);

        // Move body
        var pos = new Vector2(Body.Position.X, Body.Position.Y);
        pos += _velMps * dt;
        Body.Position = new AetherVec2(pos.X, pos.Y);
        Body.LinearVelocity = AetherVec2.Zero;   // kinematic: zero out physics velocity

        // Record trail
        var px = new Vector2(pos.X * GameConstants.PhysicsToPixels,
                             pos.Y * GameConstants.PhysicsToPixels);
        _trail.Enqueue(px);
        if (_trail.Count > TrailLen) _trail.Dequeue();
    }

    // ── Draw (inside BeginMode2D) ────────────────────────────────────────────────
    public void Draw()
    {
        var pts = _trail.ToArray();
        if (pts.Length < 2) return;

        for (int i = 1; i < pts.Length; i++)
        {
            float t = i / (float)pts.Length;
            byte a = (byte)(t * 200);
            byte r = (byte)(TrailEnd.R + (TrailStart.R - TrailEnd.R) * t);
            byte g = (byte)(TrailEnd.G + (TrailStart.G - TrailEnd.G) * t);
            byte b = (byte)(TrailEnd.B + (TrailStart.B - TrailEnd.B) * t);
            Raylib.DrawLineEx(pts[i - 1], pts[i], 2.5f, new Color(r, g, b, a));
        }

        // Bullet head
        if (pts.Length > 0)
            Raylib.DrawCircleV(pts[^1], 4f, HeadColor);
    }

    // ── Collision callback ───────────────────────────────────────────────────────
    private bool OnCollision(Fixture self, Fixture other, Contact contact)
    {
        if (_hitSomething) return false;

        _hitSomething = true;

        if (other.Body.Tag is "player")
        {
            HitPlayer     = true;
            HitPlayerBody = other.Body;
            HitPointMeters = new Vector2(Body.Position.X, Body.Position.Y);
        }

        return true;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────────
    public void Destroy(PhysicsWorld pw)
    {
        Body.OnCollision -= OnCollision;
        pw.World.Remove(Body);
    }
}

/// <summary>
/// Physics collision bitmask categories.
/// </summary>
public static class PhysicsCategories
{
    public const nkast.Aether.Physics2D.Dynamics.Category
        All    = nkast.Aether.Physics2D.Dynamics.Category.All,
        Bullet = nkast.Aether.Physics2D.Dynamics.Category.Cat2,
        Player = nkast.Aether.Physics2D.Dynamics.Category.Cat3,
        World  = nkast.Aether.Physics2D.Dynamics.Category.Cat4;
}
