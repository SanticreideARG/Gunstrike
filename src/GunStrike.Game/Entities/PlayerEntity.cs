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
    private float _walkPhase;

    public float Health    { get; private set; } = GameConstants.PlayerMaxHealth;
    public float MaxHealth => GameConstants.PlayerMaxHealth;
    public bool  IsAlive   => Health > 0f;

    // ── Reload animation ──────────────────────────────────────────────────────
    public  bool  IsReloading   { get; private set; }
    private float _reloadTimer;
    private float _reloadStartAngle;   // aim angle when reload began
    private float _reloadLowAngle;     // "weapon lowered" target angle
    private float _reloadTargetAngle;  // current mouse aim (updated each frame during phase 3)

    /// Duration in seconds — later read from equipped WeaponData.ReloadTime
    private const float ReloadDuration = 2.0f;

    /// Progress 0..1, exposed for HUD.
    public float ReloadProgress =>
        IsReloading ? Math.Clamp(_reloadTimer / ReloadDuration, 0f, 1f) : 0f;

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

        // Start reload: only when active and not already reloading
        if (input.Reload && !IsRagdoll && !IsReloading)
            StartReload();

        UpdateGroundDetection();

        if (!IsRagdoll)
            UpdateActive(dt, input);

        if (IsReloading)
            UpdateReload(dt, input);   // drives _aimAngle; also tracks mouse for phase 3
        else
            UpdateAim(input);          // normal mouse tracking
    }

    private void UpdateActive(float dt, InputState input)
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

        // Walk cycle phase — drives leg animation
        float walkVx = torso.LinearVelocity.X;
        if (MathF.Abs(walkVx) > 0.2f)
            _walkPhase += dt * MathF.Abs(walkVx) * 3.4f;
        else
            _walkPhase = float.Lerp(_walkPhase, 0f, dt * 14f);
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

    // ── Reload animation ──────────────────────────────────────────────────────

    private void StartReload()
    {
        IsReloading        = true;
        _reloadTimer       = 0f;
        _reloadStartAngle  = _aimAngle;
        _reloadTargetAngle = _aimAngle;

        // "Lowered" position: arm drops forward-and-down relative to facing
        bool facingRight = _aimAngle is > -MathF.PI / 2f and < MathF.PI / 2f;
        _reloadLowAngle  = facingRight
            ? MathF.PI * 0.44f    // ≈ 79° — barrel points down-forward (right)
            : MathF.PI * 0.56f;   // ≈ 101° — mirrored for left-facing
    }

    /// <summary>
    /// Three-phase arm animation:
    ///   0–30 %  → lower weapon (aim → low angle, smooth ease-out)
    ///  30–70 %  → hold low with a gentle wobble (magazine swap)
    ///  70–100 % → raise weapon back (low angle → current mouse aim, smooth ease-in)
    /// </summary>
    private void UpdateReload(float dt, InputState input)
    {
        _reloadTimer += dt;
        float t = Math.Clamp(_reloadTimer / ReloadDuration, 0f, 1f);

        // Keep tracking the mouse so the weapon returns to the correct position
        if (input.AimDirection.LengthSquared() > 0.001f)
            _reloadTargetAngle = MathF.Atan2(input.AimDirection.Y, input.AimDirection.X);

        const float p1End = 0.30f;   // lower phase ends
        const float p2End = 0.68f;   // hold phase ends
        // phase 3 runs p2End → 1.0

        if (t < p1End)
        {
            // Phase 1 — lower: smoothstep ease-out
            float p = Smoothstep(t / p1End);
            _aimAngle = LerpAngle(_reloadStartAngle, _reloadLowAngle, p);
        }
        else if (t < p2End)
        {
            // Phase 2 — hold & wobble (magazine eject / insert feel)
            float localT  = (t - p1End) / (p2End - p1End); // 0..1 within this phase
            float wobble  = MathF.Sin(localT * MathF.PI * 3f) * 0.07f;  // 1.5 oscillations
            _aimAngle = _reloadLowAngle + wobble;
        }
        else
        {
            // Phase 3 — raise: smoothstep ease-in
            float p = Smoothstep((t - p2End) / (1f - p2End));
            _aimAngle = LerpAngle(_reloadLowAngle, _reloadTargetAngle, p);
        }

        // Finished
        if (_reloadTimer >= ReloadDuration)
        {
            IsReloading = false;
            _aimAngle   = _reloadTargetAngle;
        }
    }

    private void CancelReload()
    {
        if (!IsReloading) return;
        IsReloading = false;
        _aimAngle   = _reloadTargetAngle;   // snap to wherever mouse was
    }

    // Easing helpers ─────────────────────────────────────────────────────────
    private static float Smoothstep(float t) => t * t * (3f - 2f * t);

    private static float LerpAngle(float a, float b, float t)
    {
        // Lerp through the shortest arc (handles 0/2π wraparound)
        float diff = ((b - a + MathF.PI * 3f) % (MathF.PI * 2f)) - MathF.PI;
        return a + diff * t;
    }

    private void UpdateAim(InputState input)
    {
        // AimDirection is pre-computed in world space by GameLoop
        if (input.AimDirection.LengthSquared() > 0.001f)
            _aimAngle = MathF.Atan2(input.AimDirection.Y, input.AimDirection.X);
    }

    private void UpdateGroundDetection()
    {
        var torso  = _parts[BodyPartId.Torso].Body;
        var feetM  = new Vector2(torso.Position.X, torso.Position.Y + 0.50f);
        var belowM = new Vector2(torso.Position.X, torso.Position.Y + 0.70f);
        _onGround  = _pw.RayCastStatic(feetM, belowM);
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
        CancelReload();   // abort any in-progress reload
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

    public void TakeDamage(float dmg)
    {
        if (!IsAlive) return;
        Health = Math.Max(0f, Health - dmg);
        if (Health <= 0f && !IsRagdoll) ToggleRagdoll();
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

        float walkP = (IsReloading || IsRagdoll) ? 0f : _walkPhase;
        _renderer.Draw(positions, rotations, aimDeg, faceRight, IsRagdoll, walkP);
    }

    public void Unload() { }

    public void Dispose() => _renderer.Dispose();
}
