using SysVec2 = System.Numerics.Vector2;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using nkast.Aether.Physics2D.Dynamics;
using GunStrike.Core;

namespace GunStrike.Physics;

/// <summary>
/// Thin wrapper around Aether/Box2D World.
/// All public APIs use System.Numerics.Vector2 (SysVec2).
/// Internal conversions handle the Aether type.
/// </summary>
public class PhysicsWorld
{
    public World World { get; }

    public PhysicsWorld()
    {
        World = new World(ToAether(new SysVec2(GameConstants.GravityX, GameConstants.GravityY)));
    }

    public void Step(float deltaSeconds)
    {
        float clamped = Math.Min(deltaSeconds, 0.05f);
        World.Step(clamped);
    }

    // ── Unit conversion (meters ↔ pixels) ──────────────────────────────────────

    public static SysVec2 ToPixels(SysVec2 physicsPos)
        => new(physicsPos.X * GameConstants.PhysicsToPixels,
               physicsPos.Y * GameConstants.PhysicsToPixels);

    public static SysVec2 ToPhysics(SysVec2 pixelPos)
        => new(pixelPos.X * GameConstants.PixelsToPhysics,
               pixelPos.Y * GameConstants.PixelsToPhysics);

    public static float ToPixels(float meters) => meters * GameConstants.PhysicsToPixels;
    public static float ToPhysics(float pixels) => pixels * GameConstants.PixelsToPhysics;

    // ── Aether ↔ System.Numerics conversion ────────────────────────────────────

    public static AetherVec2 ToAether(SysVec2 v) => new(v.X, v.Y);
    public static SysVec2    ToSys(AetherVec2 v) => new(v.X, v.Y);

    // ── Factory helpers ────────────────────────────────────────────────────────

    /// <summary>Create a static box (terrain tile). Position = center in meters.</summary>
    public Body CreateStaticBox(SysVec2 posMeters, float widthM, float heightM)
    {
        var body = World.CreateBody(ToAether(posMeters), 0f, BodyType.Static);
        body.CreateRectangle(widthM, heightM, 1f, AetherVec2.Zero);
        return body;
    }

    /// <summary>Create a dynamic rectangle body.</summary>
    public Body CreateDynamicBox(SysVec2 posMeters, float widthM, float heightM,
                                  float density = 1f, float restitution = 0f, float friction = 0.5f)
    {
        var body = World.CreateBody(ToAether(posMeters), 0f, BodyType.Dynamic);
        var fix  = body.CreateRectangle(widthM, heightM, density, AetherVec2.Zero);
        fix.Restitution = restitution;
        fix.Friction    = friction;
        return body;
    }

    /// <summary>Create a dynamic circle body.</summary>
    public Body CreateDynamicCircle(SysVec2 posMeters, float radiusM,
                                     float density = 1f, float restitution = 0.1f, float friction = 0.3f)
    {
        var body = World.CreateBody(ToAether(posMeters), 0f, BodyType.Dynamic);
        var fix  = body.CreateCircle(radiusM, density, AetherVec2.Zero);
        fix.Restitution = restitution;
        fix.Friction    = friction;
        return body;
    }

    /// <summary>Returns true if any STATIC fixture lies between from and to (meters).</summary>
    public bool RayCastStatic(SysVec2 from, SysVec2 to)
    {
        bool hit = false;
        World.RayCast((fixture, point, normal, fraction) =>
        {
            if (fixture.Body.BodyType == BodyType.Static) { hit = true; return 0f; }
            return fraction;
        }, ToAether(from), ToAether(to));
        return hit;
    }
}
