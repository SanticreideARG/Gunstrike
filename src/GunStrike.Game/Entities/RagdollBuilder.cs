using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVec2 = nkast.Aether.Physics2D.Common.Vector2;
using GunStrike.Core;
using GunStrike.Physics;

namespace GunStrike.Entities;

/// <summary>
/// Constructs the 10-segment ragdoll at 275 px height (PhysicsToPixels = 200).
///
/// Segment map (meters → pixels @ 200 px/m):
///   Head       0.25 m →  50 px   width 0.21 m → 42 px
///   Torso      0.52 m → 104 px   width 0.28 m → 56 px
///   UpperArm   0.27 m →  54 px   width 0.11 m → 22 px
///   LowerArm   0.24 m →  48 px   width 0.09 m → 18 px
///   UpperLeg   0.30 m →  60 px   width 0.13 m → 26 px
///   LowerLeg   0.28 m →  56 px   width 0.10 m → 20 px
///
/// Visual total (head + torso + upper + lower leg) = 50+104+60+56 = 270 px
/// Joint gap ~5 px each × 3 joints ≈ 275 px ✓
/// </summary>
public static class RagdollBuilder
{
    // (width m, height m, color)
    private static readonly Dictionary<BodyPartId, (float w, float h, Color c)> Defs = new()
    {
        [BodyPartId.Torso]     = (0.28f, 0.52f, new Color(50,  110, 190, 255)),
        [BodyPartId.Head]      = (0.21f, 0.25f, new Color(220, 178, 138, 255)),
        [BodyPartId.UpperArmR] = (0.11f, 0.27f, new Color(50,  110, 190, 255)),
        [BodyPartId.LowerArmR] = (0.09f, 0.24f, new Color(220, 178, 138, 255)),
        [BodyPartId.UpperArmL] = (0.11f, 0.27f, new Color(50,  110, 190, 255)),
        [BodyPartId.LowerArmL] = (0.09f, 0.24f, new Color(220, 178, 138, 255)),
        [BodyPartId.UpperLegR] = (0.13f, 0.30f, new Color(35,   75, 150, 255)),
        [BodyPartId.LowerLegR] = (0.10f, 0.28f, new Color(35,   75, 150, 255)),
        [BodyPartId.UpperLegL] = (0.13f, 0.30f, new Color(35,   75, 150, 255)),
        [BodyPartId.LowerLegL] = (0.10f, 0.28f, new Color(35,   75, 150, 255)),
    };

    private static AetherVec2 A(Vector2 v) => new(v.X, v.Y);

    public static (Dictionary<BodyPartId, BodyPart> parts, List<RevoluteJoint> joints)
        Build(PhysicsWorld pw, Vector2 spawnMeters)
    {
        var parts  = new Dictionary<BodyPartId, BodyPart>();
        var joints = new List<RevoluteJoint>();

        // ── Positions computed top-down from spawn (torso center) ───────────────
        var tp = spawnMeters;

        float tw  = Defs[BodyPartId.Torso].w;
        float th  = Defs[BodyPartId.Torso].h;
        float headH = Defs[BodyPartId.Head].h;

        // Torso
        parts[BodyPartId.Torso] = Make(pw, BodyPartId.Torso, tp);

        // Head: sits directly on torso top (small gap = joint overlap)
        parts[BodyPartId.Head] = Make(pw, BodyPartId.Head,
            tp + new Vector2(0f, -(th / 2f + headH / 2f - 0.01f)));

        // Arms: shoulder level = top of torso + small offset
        float shoulderY = tp.Y - th / 2f + 0.05f;
        float uaHalfW   = Defs[BodyPartId.UpperArmR].w / 2f;
        float uaH       = Defs[BodyPartId.UpperArmR].h;
        float laH       = Defs[BodyPartId.LowerArmR].h;

        // Right arm
        parts[BodyPartId.UpperArmR] = Make(pw, BodyPartId.UpperArmR,
            new Vector2(tp.X + tw / 2f + uaHalfW, shoulderY + uaH / 2f));
        parts[BodyPartId.LowerArmR] = Make(pw, BodyPartId.LowerArmR,
            new Vector2(tp.X + tw / 2f + uaHalfW, shoulderY + uaH + laH / 2f));

        // Left arm
        parts[BodyPartId.UpperArmL] = Make(pw, BodyPartId.UpperArmL,
            new Vector2(tp.X - tw / 2f - uaHalfW, shoulderY + uaH / 2f));
        parts[BodyPartId.LowerArmL] = Make(pw, BodyPartId.LowerArmL,
            new Vector2(tp.X - tw / 2f - uaHalfW, shoulderY + uaH + laH / 2f));

        // Legs: hip level = bottom of torso
        float hipY    = tp.Y + th / 2f;
        float legOffX = tw / 4f;
        float ulH     = Defs[BodyPartId.UpperLegR].h;
        float llH     = Defs[BodyPartId.LowerLegR].h;

        parts[BodyPartId.UpperLegR] = Make(pw, BodyPartId.UpperLegR,
            new Vector2(tp.X + legOffX, hipY + ulH / 2f));
        parts[BodyPartId.LowerLegR] = Make(pw, BodyPartId.LowerLegR,
            new Vector2(tp.X + legOffX, hipY + ulH + llH / 2f));

        parts[BodyPartId.UpperLegL] = Make(pw, BodyPartId.UpperLegL,
            new Vector2(tp.X - legOffX, hipY + ulH / 2f));
        parts[BodyPartId.LowerLegL] = Make(pw, BodyPartId.LowerLegL,
            new Vector2(tp.X - legOffX, hipY + ulH + llH / 2f));

        // ── Joints ──────────────────────────────────────────────────────────────
        var torso = parts[BodyPartId.Torso].Body;

        // Neck ±20°
        joints.Add(Join(pw, torso, parts[BodyPartId.Head].Body,
            tp + new Vector2(0f, -th / 2f), -0.35f, 0.35f));

        // Right shoulder  -90° .. +60°
        joints.Add(Join(pw, torso, parts[BodyPartId.UpperArmR].Body,
            new Vector2(tp.X + tw / 2f, shoulderY), -1.57f, 1.05f));
        // Right elbow  0° .. 130°
        joints.Add(Join(pw, parts[BodyPartId.UpperArmR].Body, parts[BodyPartId.LowerArmR].Body,
            new Vector2(tp.X + tw / 2f + uaHalfW, shoulderY + uaH), 0f, 2.27f));

        // Left shoulder  -60° .. +90°
        joints.Add(Join(pw, torso, parts[BodyPartId.UpperArmL].Body,
            new Vector2(tp.X - tw / 2f, shoulderY), -1.05f, 1.57f));
        // Left elbow  -130° .. 0°
        joints.Add(Join(pw, parts[BodyPartId.UpperArmL].Body, parts[BodyPartId.LowerArmL].Body,
            new Vector2(tp.X - tw / 2f - uaHalfW, shoulderY + uaH), -2.27f, 0f));

        // Right hip  -30° .. +80°
        joints.Add(Join(pw, torso, parts[BodyPartId.UpperLegR].Body,
            new Vector2(tp.X + legOffX, hipY), -0.52f, 1.40f));
        // Right knee  -120° .. 0°
        joints.Add(Join(pw, parts[BodyPartId.UpperLegR].Body, parts[BodyPartId.LowerLegR].Body,
            new Vector2(tp.X + legOffX, hipY + ulH), -2.09f, 0f));

        // Left hip  -80° .. +30°
        joints.Add(Join(pw, torso, parts[BodyPartId.UpperLegL].Body,
            new Vector2(tp.X - legOffX, hipY), -1.40f, 0.52f));
        // Left knee  0° .. 120°
        joints.Add(Join(pw, parts[BodyPartId.UpperLegL].Body, parts[BodyPartId.LowerLegL].Body,
            new Vector2(tp.X - legOffX, hipY + ulH), 0f, 2.09f));

        return (parts, joints);
    }

    private static BodyPart Make(PhysicsWorld pw, BodyPartId id, Vector2 pos)
    {
        var (w, h, c) = Defs[id];
        var body = pw.CreateDynamicBox(pos, w, h, density: 1f, restitution: 0f, friction: 0.4f);
        body.LinearDamping  = GameConstants.RagdollLinearDamping;
        body.AngularDamping = GameConstants.RagdollAngularDamping;
        return new BodyPart(id, body, w, h, c);
    }

    private static RevoluteJoint Join(
        PhysicsWorld pw, Body a, Body b,
        Vector2 worldAnchor, float lower, float upper)
    {
        var j = JointFactory.CreateRevoluteJoint(pw.World, a, b, A(worldAnchor));
        j.LimitEnabled     = true;
        j.LowerLimit       = lower;
        j.UpperLimit       = upper;
        j.CollideConnected = false;
        return j;
    }
}
