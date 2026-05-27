using System.Numerics;
using Raylib_cs;
using GunStrike.Core;
using GunStrike.Entities;

namespace GunStrike.Rendering;

/// <summary>
/// Draws the player character using generated pixel-art sprites.
///
/// HYBRID orientation (recommended approach):
///   - Lower body (legs) : flip based on movement direction (_facingRight).
///   - Upper body (torso, arms) : rotate based on aim angle.
///   - Head : follows torso flip + slight aim-based tilt.
///
/// Z-order (back to front):
///   Left arm back → Left leg → Right leg → Torso → Head → Right arm front
///
/// Ragdoll mode:
///   Each part's world position AND rotation comes from its physics Body,
///   so sprites follow the ragdoll naturally. Flip is locked to last direction.
/// </summary>
public class PlayerRenderer : IDisposable
{
    private readonly Dictionary<BodyPartId, CharacterSprite> _sprites;
    private bool _disposed;

    // Z-order layers (lower index = drawn first / behind)
    private static readonly BodyPartId[] DrawOrder =
    [
        BodyPartId.LowerArmL, BodyPartId.UpperArmL,   // back arm
        BodyPartId.LowerLegL, BodyPartId.UpperLegL,   // back leg
        BodyPartId.LowerLegR, BodyPartId.UpperLegR,   // front leg
        BodyPartId.Torso,
        BodyPartId.Head,
        BodyPartId.UpperArmR, BodyPartId.LowerArmR,   // front arm (weapon arm)
    ];

    public PlayerRenderer()
    {
        _sprites = PlayerSpriteFactory.CreateAll();
    }

    /// <summary>
    /// Draw the full character.
    /// partPositions : world pixel position of each part's physics body center.
    /// partRotations : rotation in radians for each part.
    /// aimAngleDeg   : mouse aim angle in degrees (used for upper-body rotation).
    /// facingRight   : flips all sprites horizontally when false.
    /// isRagdoll     : when true, each part follows its own physics rotation.
    /// </summary>
    public void Draw(
        Dictionary<BodyPartId, Vector2> partPositions,
        Dictionary<BodyPartId, float>   partRotations,
        float  aimAngleDeg,
        bool   facingRight,
        bool   isRagdoll)
    {
        foreach (var id in DrawOrder)
        {
            if (!partPositions.TryGetValue(id, out var pos)) continue;
            if (!partRotations.TryGetValue(id, out var rot)) continue;
            if (!_sprites.TryGetValue(id, out var sprite))   continue;

            float angleDeg;

            if (isRagdoll)
            {
                // Pure physics rotation for all parts
                angleDeg = rot * (180f / MathF.PI);
            }
            else
            {
                angleDeg = GetActiveAngle(id, rot, aimAngleDeg, facingRight);
            }

            sprite.Draw(pos, angleDeg, facingRight);
        }
    }

    /// <summary>
    /// Returns the draw angle for a part in active (non-ragdoll) mode.
    /// Lower body parts stay upright; upper body follows aim.
    /// </summary>
    private static float GetActiveAngle(BodyPartId id, float physicsRot,
                                         float aimDeg, bool facingRight)
    {
        return id switch
        {
            // Legs stay vertical (physics keeps them upright in active mode)
            BodyPartId.UpperLegR or BodyPartId.UpperLegL
                or BodyPartId.LowerLegR or BodyPartId.LowerLegL => 0f,

            // Torso tilts slightly toward aim (10% of aim angle)
            BodyPartId.Torso => aimDeg * 0.08f,

            // Head: slight aim tilt
            BodyPartId.Head  => aimDeg * 0.05f,

            // Front arm (weapon arm = R): full aim angle
            BodyPartId.UpperArmR or BodyPartId.LowerArmR
                => facingRight ? aimDeg : 180f - aimDeg,

            // Back arm (L): slight counter-rotation for balance
            BodyPartId.UpperArmL or BodyPartId.LowerArmL
                => facingRight ? aimDeg * 0.3f + 20f
                               : -(aimDeg * 0.3f + 20f),

            _ => 0f
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var s in _sprites.Values) s.Dispose();
        _disposed = true;
    }
}
