using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using GunStrike.Core;
using GunStrike.Input;
using GunStrike.Physics;
using GunStrike.Rendering;

namespace GunStrike.Entities;

/// <summary>
/// Player character.
/// ACTIVE mode: torso is Dynamic, limbs are Kinematic (follow torso rigidly).
/// RAGDOLL mode: all bodies Dynamic, joints free — physics takes over.
/// </summary>
public class PlayerEntity : IDisposable
{
    private readonly PhysicsWorld _pw;
    private PlayerRenderer _renderer = null!;
    private Dictionary<BodyPartId, BodyPart> _parts = [];
    private List<RevoluteJoint> _joints = [];

    private bool  _onGround;
    private float _aimAngle;   // radians from +X
    private bool  _facingRight = true;

    public bool    IsRagdoll     { get; private set; }

    /// <summary>Torso center in pixels (world space).</summary>
    public Vector2 PixelPosition => _parts[BodyPartId.Torso].PixelPosition;

    /// <summary>Torso center in meters (physics space).</summary>
    public Vector2 TorsoMeters
    {
        get { var b = _parts[BodyPartId.Torso].Body; return new Vector2(b.Position.X, b.Position.Y); }
    }

    /// <summary>
    /// Muzzle position in meters — tip of the front hand, offset along aim direction.
    /// Used as bullet spawn origin.
    /// </summary>
    public Vector2 MuzzleMeters
    {
        get
        {
            var tp = TorsoMeters;
            // Offset: half torso width + arm length (~0.51m) in aim direction
            float reach = 0.28f / 2f + 0.27f + 0.24f + 0.05f;
            return tp + new Vector2(MathF.Cos(_aimAngle), MathF.Sin(_aimAngle)) * reach;
        }
    }

    /// <summary>Normalized aim direction vector.</summary>
    public Vector2 AimDirection =>
        new(MathF.Cos(_aimAngle), MathF.Sin(_aimAngle));

    private readonly Vector2 _spawnPos;

    public PlayerEntity(PhysicsWorld pw, Vector2 spawnMeters)
    {
        _pw       = pw;
        _spawnPos = spawnMeters;
    }

    public void Load()
    {
        // Sprites require an active OpenGL context (called after InitWindow)
        _renderer = new PlayerRenderer();
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
        if (input.MoveLeft)  { vx = -GameConstants.PlayerMoveSpeed; _facingRight = false; }
        if (input.MoveRight) { vx =  GameConstants.PlayerMoveSpeed; _facingRight = true;  }

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
        // AimDirection is pre-computed in world space by GameLoop
        if (input.AimDirection.LengthSquared() > 0.001f)
            _aimAngle = MathF.Atan2(input.AimDirection.Y, input.AimDirection.X);
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
        // Collect world-pixel positions and physics rotations for each part
        var positions  = new Dictionary<BodyPartId, Vector2>();
        var rotations  = new Dictionary<BodyPartId, float>();

        foreach (var (id, part) in _parts)
        {
            positions[id] = part.PixelPosition;
            rotations[id] = part.Rotation;
        }

        float aimDeg = _aimAngle * (180f / MathF.PI);

        // Flip: face the direction we're aiming at (left vs right half)
        bool faceRight = _aimAngle is > -MathF.PI / 2f and < MathF.PI / 2f;

        _renderer.Draw(positions, rotations, aimDeg, faceRight, IsRagdoll);
    }

    public void Unload() { }

    public void Dispose() => _renderer.Dispose();
}
