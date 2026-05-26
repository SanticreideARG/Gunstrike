using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using GunStrike.Core;
using GunStrike.Input;
using GunStrike.Physics;

namespace GunStrike.Entities;

/// <summary>
/// Player character.
/// ACTIVE mode: torso is Dynamic, limbs are Kinematic (follow torso rigidly).
/// RAGDOLL mode: all bodies Dynamic, joints free — physics takes over.
/// </summary>
public class PlayerEntity
{
    private readonly PhysicsWorld _pw;
    private Dictionary<BodyPartId, BodyPart> _parts = [];
    private List<RevoluteJoint> _joints = [];

    private bool  _onGround;
    private float _aimAngle;   // radians from +X

    public bool    IsRagdoll     { get; private set; }
    public Vector2 PixelPosition => _parts[BodyPartId.Torso].PixelPosition;

    private readonly Vector2 _spawnPos;

    public PlayerEntity(PhysicsWorld pw, Vector2 spawnMeters)
    {
        _pw       = pw;
        _spawnPos = spawnMeters;
    }

    public void Load()
    {
        (_parts, _joints) = RagdollBuilder.Build(_pw, _spawnPos);
        SetActiveMode();
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    public void Update(float dt, InputState input)
    {
        if (input.ToggleRagdoll) ToggleRagdoll();

        UpdateGroundDetection();

        if (!IsRagdoll)
            UpdateActive(input);

        UpdateAim(input);
    }

    private void UpdateActive(InputState input)
    {
        var torso = _parts[BodyPartId.Torso].Body;
        var vel   = torso.LinearVelocity;

        float vx = 0f;
        if (input.MoveLeft)  vx = -GameConstants.PlayerMoveSpeed;
        if (input.MoveRight) vx =  GameConstants.PlayerMoveSpeed;

        torso.LinearVelocity = new AetherVec2(vx, vel.Y);

        if (input.Jump && _onGround)
            torso.LinearVelocity = new AetherVec2(vel.X, -GameConstants.PlayerJumpImpulse);

        // Clamp fall
        if (torso.LinearVelocity.Y > GameConstants.PlayerMaxFallSpeed)
            torso.LinearVelocity = new AetherVec2(torso.LinearVelocity.X, GameConstants.PlayerMaxFallSpeed);

        SyncLimbsToTorso();
    }

    private void SyncLimbsToTorso()
    {
        var torso = _parts[BodyPartId.Torso].Body;
        var tp    = new Vector2(torso.Position.X, torso.Position.Y);
        const float th = 0.45f, tw = 0.30f;

        MoveKinematic(_parts[BodyPartId.Head].Body,       tp + new Vector2(0,              -(th / 2f + 0.11f)));
        MoveKinematic(_parts[BodyPartId.UpperArmR].Body,  tp + new Vector2( tw / 2f + 0.06f, -th / 2f + 0.10f));
        MoveKinematic(_parts[BodyPartId.LowerArmR].Body,  tp + new Vector2( tw / 2f + 0.06f, -th / 2f + 0.38f));
        MoveKinematic(_parts[BodyPartId.UpperArmL].Body,  tp + new Vector2(-tw / 2f - 0.06f, -th / 2f + 0.10f));
        MoveKinematic(_parts[BodyPartId.LowerArmL].Body,  tp + new Vector2(-tw / 2f - 0.06f, -th / 2f + 0.38f));
        MoveKinematic(_parts[BodyPartId.UpperLegR].Body,  tp + new Vector2( tw / 4f,           th / 2f + 0.16f));
        MoveKinematic(_parts[BodyPartId.LowerLegR].Body,  tp + new Vector2( tw / 4f,           th / 2f + 0.48f));
        MoveKinematic(_parts[BodyPartId.UpperLegL].Body,  tp + new Vector2(-tw / 4f,           th / 2f + 0.16f));
        MoveKinematic(_parts[BodyPartId.LowerLegL].Body,  tp + new Vector2(-tw / 4f,           th / 2f + 0.48f));
    }

    private static void MoveKinematic(Body body, Vector2 pos)
    {
        body.Position        = new AetherVec2(pos.X, pos.Y);
        body.LinearVelocity  = AetherVec2.Zero;
        body.AngularVelocity = 0f;
        body.Rotation        = 0f;
    }

    private void UpdateAim(InputState input)
    {
        var dir = input.MouseScreenPos - PixelPosition;
        if (dir.LengthSquared() > 0.01f)
            _aimAngle = MathF.Atan2(dir.Y, dir.X);
    }

    private void UpdateGroundDetection()
    {
        // Simple velocity heuristic — will be replaced by contact listener
        _onGround = MathF.Abs(_parts[BodyPartId.Torso].Body.LinearVelocity.Y) < 0.5f;
    }

    // ── Ragdoll ──────────────────────────────────────────────────────────────────

    public void ToggleRagdoll()
    {
        IsRagdoll = !IsRagdoll;
        if (IsRagdoll) SetRagdollMode();
        else           SetActiveMode();
    }

    private void SetRagdollMode()
    {
        foreach (var j in _joints) j.LimitEnabled = false;

        foreach (var (_, p) in _parts)
        {
            p.Body.BodyType       = BodyType.Dynamic;
            p.Body.LinearDamping  = GameConstants.RagdollLinearDamping;
            p.Body.AngularDamping = GameConstants.RagdollAngularDamping;
        }
    }

    private void SetActiveMode()
    {
        foreach (var j in _joints) j.LimitEnabled = true;

        foreach (var (id, p) in _parts)
        {
            p.Body.BodyType = (id == BodyPartId.Torso || id == BodyPartId.UpperLegR || id == BodyPartId.UpperLegL)
                ? BodyType.Dynamic
                : BodyType.Kinematic;
            p.Body.AngularVelocity = 0f;
            p.Body.Rotation        = 0f;
        }
    }

    /// <summary>Apply ballistic impact — forces ragdoll on the nearest body part.</summary>
    public void ApplyImpact(Vector2 worldPointMeters, Vector2 impulseMeters)
    {
        IsRagdoll = true;
        SetRagdollMode();

        BodyPart? closest = null;
        float     minDist = float.MaxValue;

        foreach (var (_, p) in _parts)
        {
            var pos = new Vector2(p.Body.Position.X, p.Body.Position.Y);
            float d = Vector2.Distance(pos, worldPointMeters);
            if (d < minDist) { minDist = d; closest = p; }
        }

        if (closest is not null)
        {
            var anchor = new AetherVec2(worldPointMeters.X, worldPointMeters.Y);
            var imp    = new AetherVec2(impulseMeters.X, impulseMeters.Y);
            closest.Body.ApplyLinearImpulse(imp, anchor);
        }
    }

    // ── Draw ─────────────────────────────────────────────────────────────────────

    public void Draw()
    {
        // Back-to-front Z order (left limbs behind, right limbs in front)
        BodyPartId[] order =
        [
            BodyPartId.LowerLegL, BodyPartId.UpperLegL,
            BodyPartId.LowerArmL, BodyPartId.UpperArmL,
            BodyPartId.Torso,
            BodyPartId.Head,
            BodyPartId.UpperLegR, BodyPartId.LowerLegR,
            BodyPartId.UpperArmR, BodyPartId.LowerArmR,
        ];

        foreach (var id in order)
            _parts[id].Draw();

        DrawAimLine();
    }

    private void DrawAimLine()
    {
        var origin = PixelPosition;
        var tip    = origin + new Vector2(MathF.Cos(_aimAngle), MathF.Sin(_aimAngle)) * 80f;
        Raylib.DrawLineEx(origin, tip, 2f, new Color(255, 200, 50, 200));
        Raylib.DrawCircleV(tip, 4f, Color.Yellow);
    }

    public void Unload() { }
}
