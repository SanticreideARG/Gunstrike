using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using GunStrike.Core;
using GunStrike.Data;
using GunStrike.Physics;

namespace GunStrike.Entities;

public enum EnemyAIState { Patrol, Alert, Chase, Attack, Dead }

/// <summary>
/// Single enemy entity. Dynamic physics body, AI state machine, hitscan attack.
/// </summary>
public class EnemyEntity
{
    // ── Physics ───────────────────────────────────────────────────────────────
    private readonly PhysicsWorld _pw;
    private readonly Body         _body;

    public Body    PhysicsBody    => _body;
    public Vector2 PositionMeters => new(_body.Position.X, _body.Position.Y);

    // ── Data ──────────────────────────────────────────────────────────────────
    private readonly EnemyData _data;
    private float _health;

    // ── Dimensions ───────────────────────────────────────────────────────────
    private const float BodyW     = 0.30f;
    private const float BodyH     = 0.75f;
    private const float BodyHalfH = BodyH / 2f;

    // ── AI ────────────────────────────────────────────────────────────────────
    private EnemyAIState _state      = EnemyAIState.Patrol;
    private float        _stateTimer;
    private float        _shootTimer;
    private bool         _facingRight = true;
    private float        _aimAngle;

    // ── Patrol ────────────────────────────────────────────────────────────────
    private readonly float _patrolOriginX;
    private readonly float _patrolRange;
    private float          _patrolDir = 1f;

    // ── Death ─────────────────────────────────────────────────────────────────
    private float _deadTimer;
    private const float DeadFadeTime = 3.0f;

    // ── Public state ──────────────────────────────────────────────────────────
    public bool  IsDead       { get; private set; }
    public bool  ShouldRemove => IsDead && _deadTimer >= DeadFadeTime;
    public float DeadAlpha    => IsDead ? Math.Max(0f, 1f - _deadTimer / DeadFadeTime) : 1f;

    /// <summary>Callback fired when this enemy scores a hitscan hit on the player.</summary>
    public Action<float>? OnHitPlayer;

    private static readonly Random _rng = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public EnemyEntity(PhysicsWorld pw, EnemyData data, Vector2 spawnMeters, float patrolRange = 4f)
    {
        _pw            = pw;
        _data          = data;
        _health        = data.Stats.MaxHealth;
        _patrolOriginX = spawnMeters.X;
        _patrolRange   = patrolRange;

        // Dynamic body — fixed rotation in active mode
        _body = pw.CreateDynamicBox(spawnMeters, BodyW, BodyH, density: 1f,
                                    restitution: 0f, friction: 0.5f);
        _body.FixedRotation = true;
        _body.Tag           = "enemy";

        // Collision: Cat5 — no player collision, but hit by bullets
        foreach (var fix in _body.FixtureList)
        {
            fix.CollisionCategories = PhysicsCategories.Enemy;
            fix.CollidesWith = nkast.Aether.Physics2D.Dynamics.Category.All
                             ^ PhysicsCategories.Enemy;
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public void Update(float dt, Vector2 playerMeters, bool playerAlive)
    {
        if (IsDead) { _deadTimer += dt; return; }

        float dist = Vector2.Distance(PositionMeters, playerMeters);

        _shootTimer = Math.Max(0f, _shootTimer - dt);
        _stateTimer += dt;

        switch (_state)
        {
            case EnemyAIState.Patrol: UpdatePatrol(dt, playerMeters, dist); break;
            case EnemyAIState.Alert:  UpdateAlert (dt, playerMeters, dist); break;
            case EnemyAIState.Chase:  UpdateChase (dt, playerMeters, dist); break;
            case EnemyAIState.Attack: UpdateAttack(dt, playerMeters, dist, playerAlive); break;
        }

        // Aim angle: always track player when not patrolling
        if (_state != EnemyAIState.Patrol)
            _aimAngle = MathF.Atan2(playerMeters.Y - PositionMeters.Y,
                                     playerMeters.X - PositionMeters.X);
        else
            _aimAngle = _facingRight ? 0f : MathF.PI;

        // Face direction based on velocity or aim
        if (_body.LinearVelocity.X > 0.1f)  _facingRight = true;
        if (_body.LinearVelocity.X < -0.1f) _facingRight = false;
    }

    // ── AI states ─────────────────────────────────────────────────────────────

    private void UpdatePatrol(float dt, Vector2 playerMeters, float dist)
    {
        float walkSpeed = _data.AI.WalkSpeed;
        float vx = _patrolDir * walkSpeed;
        SetVx(vx);

        // Flip at patrol bounds
        float relX = _body.Position.X - _patrolOriginX;
        if (relX >  _patrolRange) _patrolDir = -1f;
        if (relX < -_patrolRange) _patrolDir =  1f;

        // Transition: heard/saw player
        bool playerClose     = dist < _data.AI.SightRange * 1.5f;
        bool playerVeryClose = dist < _data.AI.HearRange;
        if ((playerClose || playerVeryClose) && _stateTimer > 0.5f)
            SetState(EnemyAIState.Alert);
    }

    private void UpdateAlert(float dt, Vector2 playerMeters, float dist)
    {
        SetVx(0f);  // stop and look

        if (_stateTimer >= _data.AI.ReactionTime)
        {
            if (dist < _data.AI.SightRange)
                SetState(EnemyAIState.Chase);
            else
                SetState(EnemyAIState.Patrol);
        }
    }

    private void UpdateChase(float dt, Vector2 playerMeters, float dist)
    {
        float dirX = playerMeters.X > _body.Position.X ? 1f : -1f;
        SetVx(dirX * _data.AI.RunSpeed);

        if (dist <= _data.AI.AttackRange)
            SetState(EnemyAIState.Attack);
        else if (dist > _data.AI.SightRange && _stateTimer > 2f)
            SetState(EnemyAIState.Patrol);
    }

    private void UpdateAttack(float dt, Vector2 playerMeters, float dist, bool playerAlive)
    {
        SetVx(0f);  // stand still and fire

        if (dist > _data.AI.AttackRange * 1.4f)
        {
            SetState(EnemyAIState.Chase);
            return;
        }

        // Hitscan shoot
        if (playerAlive && _shootTimer <= 0f)
        {
            _shootTimer = _data.AI.AttackInterval;
            bool hit = _rng.NextSingle() > _data.AI.Inaccuracy;
            if (hit) OnHitPlayer?.Invoke(_data.Stats.AttackDamage);
        }
    }

    private void SetState(EnemyAIState s) { _state = s; _stateTimer = 0f; }

    private void SetVx(float vx)
        => _body.LinearVelocity = new AetherVec2(vx, _body.LinearVelocity.Y);

    // ── Damage ────────────────────────────────────────────────────────────────

    public void TakeDamage(float dmg, Vector2 hitPointMeters)
    {
        if (IsDead) return;
        _health -= dmg;
        if (_health <= 0f) Die(hitPointMeters);
    }

    private void Die(Vector2 hitPointMeters)
    {
        IsDead = true;
        _body.FixedRotation = false;
        // Tumble direction: away from hit point
        float tumbleDir = hitPointMeters.X < _body.Position.X ? 1f : -1f;
        _body.AngularVelocity = tumbleDir * 4f;
        _body.LinearVelocity  = new AetherVec2(tumbleDir * 1.5f, -1.5f);
    }

    // ── Draw (inside BeginMode2D) ─────────────────────────────────────────────

    public void Draw()
    {
        byte alpha = (byte)(255 * DeadAlpha);
        if (alpha == 0) return;

        float px  = _body.Position.X * GameConstants.PhysicsToPixels;
        float py  = _body.Position.Y * GameConstants.PhysicsToPixels;
        float rot = _body.Rotation * (180f / MathF.PI);

        // Class color
        var col  = ClassColor(_data.Class, alpha);
        var dark = new Color((int)(col.R * 0.6f), (int)(col.G * 0.6f), (int)(col.B * 0.6f), (int)alpha);

        float bwPx = BodyW * GameConstants.PhysicsToPixels;
        float bhPx = BodyH * GameConstants.PhysicsToPixels;
        float headR = 14f;

        if (!IsDead)
        {
            // Body (axis-aligned)
            Raylib.DrawRectangle(
                (int)(px - bwPx / 2f), (int)(py - bhPx / 2f),
                (int)bwPx, (int)bhPx, col);

            // Head
            Raylib.DrawCircle(
                (int)px, (int)(py - bhPx / 2f - headR),
                headR, col);

            // Aim line (attack state)
            if (_state == EnemyAIState.Attack)
            {
                float aimLen = 40f;
                Raylib.DrawLineEx(
                    new Vector2(px, py),
                    new Vector2(px + MathF.Cos(_aimAngle) * aimLen,
                                py + MathF.Sin(_aimAngle) * aimLen),
                    2f, new Color(220, 60, 60, (int)(180 * DeadAlpha)));
            }

            // State badge
            string badge = _state switch
            {
                EnemyAIState.Patrol => "P",
                EnemyAIState.Alert  => "!",
                EnemyAIState.Chase  => ">>",
                EnemyAIState.Attack => "X",
                _                   => ""
            };
            int tw = Raylib.MeasureText(badge, 10);
            Raylib.DrawText(badge,
                (int)(px - tw / 2f),
                (int)(py - bhPx / 2f - headR * 2f - 12f),
                10, new Color(255, 255, 255, (int)alpha));

            // Health bar
            float hpFrac = Math.Clamp(_health / _data.Stats.MaxHealth, 0f, 1f);
            float barW   = bwPx + 10f;
            float barX   = px - barW / 2f;
            float barY   = py - bhPx / 2f - headR * 2f - 28f;
            Raylib.DrawRectangle((int)barX, (int)barY, (int)barW, 5, new Color(50, 20, 20, (int)alpha));
            Raylib.DrawRectangle((int)barX, (int)barY, (int)(barW * hpFrac), 5,
                                 new Color(60, 200, 80, (int)alpha));
        }
        else
        {
            // Dead: draw rotated body
            Raylib.DrawRectanglePro(
                new Rectangle(px, py, bwPx, bhPx),
                new Vector2(bwPx / 2f, bhPx / 2f),
                rot, new Color(col.R, col.G, col.B, (int)alpha));
        }
    }

    // ── Color helper ─────────────────────────────────────────────────────────

    private static Color ClassColor(EnemyClass cls, byte alpha) => cls switch
    {
        EnemyClass.Grunt      => new Color(80,  160, 100, (int)alpha),
        EnemyClass.Heavy      => new Color(180, 120,  40, (int)alpha),
        EnemyClass.Sniper     => new Color( 70, 160, 200, (int)alpha),
        EnemyClass.Grenadier  => new Color(160, 180,  50, (int)alpha),
        EnemyClass.Boss       => new Color(200,  50,  50, (int)alpha),
        _                     => new Color(140, 140, 140, (int)alpha),
    };

    // ── Cleanup ───────────────────────────────────────────────────────────────

    public void Destroy() => _pw.World.Remove(_body);
}
