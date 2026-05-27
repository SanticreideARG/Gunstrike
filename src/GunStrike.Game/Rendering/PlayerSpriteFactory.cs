using System.Numerics;
using Raylib_cs;
using GunStrike.Core;
using GunStrike.Entities;

namespace GunStrike.Rendering;

/// <summary>
/// Generates pixel-art style CharacterSprites for each body part.
///
/// Style: chunky military soldier — think Metal Slug but blockier.
/// All sizes are derived from GameConstants segment dimensions × PhysicsToPixels.
///
/// Pivot points are set at the joint anchor of each segment:
///   Head      → center-bottom  (neck)
///   Torso     → center-top     (neck) — but drawn from center for physics alignment
///   UpperArm  → center-top     (shoulder)
///   LowerArm  → center-top     (elbow)
///   UpperLeg  → center-top     (hip)
///   LowerLeg  → center-top     (knee)
/// </summary>
public static class PlayerSpriteFactory
{
    // ── Palette ──────────────────────────────────────────────────────────────────
    private static readonly Color Skin       = new(220, 170, 130, 255);
    private static readonly Color SkinDark   = new(190, 145, 105, 255);
    private static readonly Color ArmorBlue  = new( 55, 100, 175, 255);
    private static readonly Color ArmorDark  = new( 35,  70, 130, 255);
    private static readonly Color ArmorLight = new( 80, 135, 210, 255);
    private static readonly Color PantsBlue  = new( 40,  65, 120, 255);
    private static readonly Color PantsDark  = new( 25,  45,  90, 255);
    private static readonly Color BootBrown  = new( 90,  65,  45, 255);
    private static readonly Color BootDark   = new( 60,  40,  25, 255);
    private static readonly Color Outline    = new( 20,  20,  30, 255);
    private static readonly Color White      = new(240, 240, 240, 255);
    private static readonly Color HelmetGray = new(100, 110, 120, 255);
    private static readonly Color EyeWhite   = new(235, 240, 245, 255);
    private static readonly Color EyeDark    = new( 40,  30,  20, 255);

    // Pixel size (px per "pixel-art pixel")
    private const int P = 2;

    // ── Public factory ───────────────────────────────────────────────────────────

    public static Dictionary<BodyPartId, CharacterSprite> CreateAll()
    {
        var S = GameConstants.PhysicsToPixels;
        return new Dictionary<BodyPartId, CharacterSprite>
        {
            [BodyPartId.Head]      = CreateHead(
                (int)(GameConstants.SegHead * S),
                (int)(GameConstants.SegHead * S)),

            [BodyPartId.Torso]     = CreateTorso(
                (int)(GameConstants.SegTorso * S * 0.54f),  // width = 28cm
                (int)(GameConstants.SegTorso * S)),

            [BodyPartId.UpperArmR] = CreateUpperArm(
                (int)(GameConstants.SegUpperArm * S * 0.41f),
                (int)(GameConstants.SegUpperArm * S)),

            [BodyPartId.LowerArmR] = CreateLowerArm(
                (int)(GameConstants.SegLowerArm * S * 0.33f),
                (int)(GameConstants.SegLowerArm * S)),

            [BodyPartId.UpperArmL] = CreateUpperArm(
                (int)(GameConstants.SegUpperArm * S * 0.41f),
                (int)(GameConstants.SegUpperArm * S)),

            [BodyPartId.LowerArmL] = CreateLowerArm(
                (int)(GameConstants.SegLowerArm * S * 0.33f),
                (int)(GameConstants.SegLowerArm * S)),

            [BodyPartId.UpperLegR] = CreateUpperLeg(
                (int)(GameConstants.SegUpperLeg * S * 0.43f),
                (int)(GameConstants.SegUpperLeg * S)),

            [BodyPartId.LowerLegR] = CreateLowerLeg(
                (int)(GameConstants.SegLowerLeg * S * 0.33f),
                (int)(GameConstants.SegLowerLeg * S)),

            [BodyPartId.UpperLegL] = CreateUpperLeg(
                (int)(GameConstants.SegUpperLeg * S * 0.43f),
                (int)(GameConstants.SegUpperLeg * S)),

            [BodyPartId.LowerLegL] = CreateLowerLeg(
                (int)(GameConstants.SegLowerLeg * S * 0.33f),
                (int)(GameConstants.SegLowerLeg * S)),
        };
    }

    // ── HEAD ─────────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateHead(int w, int h)
    {
        // Pivot = center-bottom (neck joint)
        var pivot = new Vector2(w / 2f, h);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            int m = 3;  // margin

            // Helmet
            FillRect(m, 0, sw - m * 2, sh / 2, HelmetGray);
            FillRect(m - 2, sh / 4, sw - (m - 2) * 2, sh / 4 + 2, ArmorDark);

            // Face
            FillRect(m, sh / 2, sw - m * 2, sh / 2 - m, Skin);

            // Eyes
            int eyeY = sh / 2 + 4;
            FillRect(sw / 2 - 7, eyeY,     5, 5, EyeWhite);
            FillRect(sw / 2 + 2, eyeY,     5, 5, EyeWhite);
            FillRect(sw / 2 - 6, eyeY + 1, 3, 3, EyeDark);
            FillRect(sw / 2 + 3, eyeY + 1, 3, 3, EyeDark);

            // Helmet brim
            FillRect(m - 2, sh / 2 - 2, sw - (m - 2) * 2, P, ArmorDark);

            // Outline
            DrawOutlineRect(m, 0, sw - m * 2, sh - m, Outline);
        });
    }

    // ── TORSO ────────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateTorso(int w, int h)
    {
        // Pivot = center of physics body (torso is the reference)
        var pivot = new Vector2(w / 2f, h / 2f);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            // Main armor plate
            FillRect(0, 0, sw, sh, ArmorBlue);

            // Chest highlight
            FillRect(P, P, sw - P * 2, sh / 3, ArmorLight);

            // Belt line
            int beltY = sh * 3 / 4;
            FillRect(0, beltY, sw, P * 2, ArmorDark);

            // Chest detail lines
            FillRect(sw / 2 - P, P * 3, P * 2, sh / 2 - P * 2, ArmorDark);

            // Shoulder pads hint
            FillRect(0,      0, P * 3, sh / 4, ArmorDark);
            FillRect(sw - P * 3, 0, P * 3, sh / 4, ArmorDark);

            // Outline
            DrawOutlineRect(0, 0, sw, sh, Outline);
        });
    }

    // ── UPPER ARM ────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateUpperArm(int w, int h)
    {
        // Pivot = center-top (shoulder joint)
        var pivot = new Vector2(w / 2f, 0);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            FillRect(0, 0, sw, sh, ArmorBlue);
            FillRect(P, P, sw - P * 2, sh / 2, ArmorLight);
            FillRect(P, sh / 2, sw - P * 2, sh / 2 - P, ArmorDark);
            DrawOutlineRect(0, 0, sw, sh, Outline);
        });
    }

    // ── LOWER ARM ────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateLowerArm(int w, int h)
    {
        // Pivot = center-top (elbow joint)
        var pivot = new Vector2(w / 2f, 0);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            // Forearm — skin + glove hint at bottom
            FillRect(0, 0,            sw, sh * 2 / 3, Skin);
            FillRect(0, sh * 2 / 3,   sw, sh / 3,     ArmorDark);  // glove

            FillRect(P, P, sw - P * 2, sh / 3, SkinDark);          // shadow
            DrawOutlineRect(0, 0, sw, sh, Outline);
        });
    }

    // ── UPPER LEG ────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateUpperLeg(int w, int h)
    {
        // Pivot = center-top (hip joint)
        var pivot = new Vector2(w / 2f, 0);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            FillRect(0, 0, sw, sh, PantsBlue);
            FillRect(P, P, sw - P * 2, sh / 2, new Color(55, 85, 145, 255));  // highlight
            FillRect(P, sh / 2, sw - P * 2, sh / 2 - P, PantsDark);
            // Seam line
            FillRect(sw / 2 - P / 2, P * 2, P, sh - P * 3, PantsDark);
            DrawOutlineRect(0, 0, sw, sh, Outline);
        });
    }

    // ── LOWER LEG ────────────────────────────────────────────────────────────────
    private static CharacterSprite CreateLowerLeg(int w, int h)
    {
        // Pivot = center-top (knee joint)
        var pivot = new Vector2(w / 2f, 0);

        return new CharacterSprite(w, h, pivot, (sw, sh) =>
        {
            int bootStart = sh * 2 / 3;

            // Shin (pants)
            FillRect(0, 0,          sw, bootStart,      PantsBlue);
            FillRect(P, P, sw - P * 2, bootStart / 2,   new Color(55, 85, 145, 255));

            // Boot
            FillRect(0,       bootStart, sw,         sh - bootStart,     BootBrown);
            FillRect(P,       bootStart + P, sw - P * 2, sh - bootStart - P * 2, BootDark);
            // Boot sole — protrudes slightly on bottom right
            FillRect(-P, sh - P * 3, sw + P * 3, P * 3, BootDark);

            DrawOutlineRect(0, 0, sw, sh, Outline);
        });
    }

    // ── Drawing helpers ──────────────────────────────────────────────────────────

    private static void FillRect(int x, int y, int w, int h, Color c)
    {
        if (w <= 0 || h <= 0) return;
        Raylib.DrawRectangle(x, y, w, h, c);
    }

    private static void DrawOutlineRect(int x, int y, int w, int h, Color c)
    {
        Raylib.DrawRectangleLines(x, y, w, h, c);
    }
}
