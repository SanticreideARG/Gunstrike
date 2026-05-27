using System.Numerics;
using GunStrike.Core;
using GunStrike.Data;
using GunStrike.Editor.UI;
using Raylib_cs;

namespace GunStrike.Editor.Weapon;

/// <summary>
/// Weapon editor right-panel: procedural silhouette, stat bars, trajectory arc.
/// All drawing is in screen space (no Camera2D needed).
/// </summary>
public class WeaponPreview
{
    private WeaponData _weapon;

    public WeaponPreview(WeaponData weapon) { _weapon = weapon; }
    public void SetWeapon(WeaponData weapon) { _weapon = weapon; }

    // ── Draw ──────────────────────────────────────────────────────────────────

    public void Draw(Rectangle bounds)
    {
        // Background
        Raylib.DrawRectangleRec(bounds, new Color(18, 20, 30, 255));

        float pad  = 24f;
        float cx   = bounds.X + pad;
        float maxW = bounds.Width - pad * 2f;
        float y    = bounds.Y + pad;

        // ── Header ────────────────────────────────────────────────────────────
        y = DrawHeader(cx, y, maxW);
        y += 16f;

        // ── Silhouette ────────────────────────────────────────────────────────
        y = DrawSilhouette(cx, y, maxW);
        y += 20f;

        // ── Stat bars ─────────────────────────────────────────────────────────
        y = DrawSeparatorLabel(cx, y, maxW, "Stats");
        y += 8f;
        y = DrawStatBars(cx, y, maxW);
        y += 20f;

        // ── Trajectory ───────────────────────────────────────────────────────
        y = DrawSeparatorLabel(cx, y, maxW, "Trajectory");
        y += 8f;
        DrawTrajectory(cx, y, maxW, bounds.Y + bounds.Height - pad - y);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private float DrawHeader(float x, float y, float w)
    {
        // Weapon name (large)
        Raylib.DrawText(_weapon.Name,
            (int)x, (int)y, 22, UITheme.LabelAccent);
        y += 26f;

        // Class + fire mode
        string sub = $"{_weapon.Class}  ·  {_weapon.FireMode}  ·  {_weapon.MagSize} rds";
        Raylib.DrawText(sub, (int)x, (int)y, UITheme.FontSizeSmall, UITheme.LabelSecondary);
        y += UITheme.FontSizeSmall + 4f;

        return y;
    }

    // ── Silhouette ────────────────────────────────────────────────────────────

    private float DrawSilhouette(float cx, float y, float w)
    {
        float boxH    = 80f;
        float centerX = cx + w / 2f;
        float centerY = y + boxH / 2f;

        // Box background
        Raylib.DrawRectangle((int)cx, (int)y, (int)w, (int)boxH,
                             new Color(24, 28, 42, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(cx, y, w, boxH),
                                    1f, UITheme.PanelBorder);

        DrawWeaponShape(centerX, centerY, w);

        // Class badge (top-right corner of box)
        string badge  = _weapon.Class.ToString().ToUpper();
        int    bw     = Raylib.MeasureText(badge, UITheme.FontSizeSmall);
        Raylib.DrawText(badge,
            (int)(cx + w - bw - 6),
            (int)(y + 6),
            UITheme.FontSizeSmall, ClassColor(_weapon.Class));

        return y + boxH;
    }

    private void DrawWeaponShape(float cx, float cy, float maxW)
    {
        // All shapes are drawn centered around (cx, cy)
        // Scale factor so widest weapon ≈ 60% of available width
        float scale = Math.Min(maxW * 0.6f / 140f, 1.5f);

        var col  = ClassColor(_weapon.Class);
        var dark = new Color((int)(col.R / 2), (int)(col.G / 2), (int)(col.B / 2), 255);

        switch (_weapon.Class)
        {
            case WeaponClass.Pistol:
                DrawPistol(cx, cy, scale, col, dark);
                break;
            case WeaponClass.Rifle:
                DrawRifle(cx, cy, scale, col, dark);
                break;
            case WeaponClass.Shotgun:
                DrawShotgun(cx, cy, scale, col, dark);
                break;
            case WeaponClass.Sniper:
                DrawSniper(cx, cy, scale, col, dark);
                break;
            case WeaponClass.RocketLauncher:
                DrawRpg(cx, cy, scale, col, dark);
                break;
            default:
                DrawRifle(cx, cy, scale, col, dark);
                break;
        }
    }

    private static void DrawPistol(float cx, float cy, float s, Color col, Color dark)
    {
        // Slide / barrel (top)
        FillRect(cx - 30*s, cy - 8*s, 60*s, 14*s, col);
        // Body
        FillRect(cx - 10*s, cy + 6*s, 28*s, 12*s, col);
        // Grip
        FillRect(cx - 2*s,  cy + 14*s, 14*s, 22*s, dark);
        // Muzzle accent
        FillRect(cx + 30*s, cy - 7*s, 4*s, 12*s, Color.DarkGray);
        // Trigger guard
        Raylib.DrawRectangleLines((int)(cx + 4*s), (int)(cy + 10*s), (int)(14*s), (int)(12*s),
                                  new Color(40, 40, 40, 180));
    }

    private static void DrawRifle(float cx, float cy, float s, Color col, Color dark)
    {
        // Stock
        FillRect(cx - 65*s, cy - 5*s,  30*s, 18*s, dark);
        // Receiver
        FillRect(cx - 35*s, cy - 8*s,  50*s, 20*s, col);
        // Barrel
        FillRect(cx + 14*s, cy - 5*s,  60*s, 10*s, col);
        // Handguard
        FillRect(cx + 14*s, cy +  4*s, 30*s,  5*s, dark);
        // Mag well
        FillRect(cx - 10*s, cy + 12*s, 16*s, 16*s, dark);
        // Muzzle
        FillRect(cx + 72*s, cy - 4*s,   6*s,  8*s, Color.DarkGray);
    }

    private static void DrawShotgun(float cx, float cy, float s, Color col, Color dark)
    {
        // Stock
        FillRect(cx - 60*s, cy - 5*s,  28*s, 20*s, dark);
        // Receiver
        FillRect(cx - 32*s, cy - 8*s,  44*s, 22*s, col);
        // Barrel (thick)
        FillRect(cx + 12*s, cy - 7*s,  55*s, 14*s, col);
        // Pump
        FillRect(cx + 18*s, cy +  6*s, 22*s,  7*s, dark);
        // Muzzle (wide)
        FillRect(cx + 64*s, cy - 8*s,   8*s, 16*s, Color.DarkGray);
    }

    private static void DrawSniper(float cx, float cy, float s, Color col, Color dark)
    {
        // Stock (angled)
        FillRect(cx - 68*s, cy - 3*s,  26*s, 14*s, dark);
        // Receiver
        FillRect(cx - 42*s, cy - 6*s,  40*s, 16*s, col);
        // Barrel (long, thin)
        FillRect(cx - 2*s,  cy - 4*s,  80*s,  8*s, col);
        // Scope
        FillRect(cx - 30*s, cy - 14*s, 30*s,  9*s, new Color(55, 55, 70, 255));
        Raylib.DrawCircleLines((int)(cx - 15*s), (int)(cy - 10*s), 6*s, new Color(100, 200, 255, 200));
        // Bipod legs
        FillRect(cx + 20*s, cy +  8*s,  3*s, 12*s, dark);
        FillRect(cx + 28*s, cy +  8*s,  3*s, 12*s, dark);
        // Muzzle brake
        FillRect(cx + 76*s, cy - 5*s,   8*s, 10*s, Color.DarkGray);
    }

    private static void DrawRpg(float cx, float cy, float s, Color col, Color dark)
    {
        // Main tube
        FillRect(cx - 60*s, cy - 7*s,  130*s, 14*s, col);
        // Sight
        FillRect(cx - 20*s, cy - 14*s,  12*s,  8*s, dark);
        // Warhead (diamond shape approximated)
        FillRect(cx + 66*s, cy - 10*s,  20*s, 20*s, new Color(200, 80, 60, 255));
        FillRect(cx + 84*s, cy -  5*s,  16*s, 10*s, new Color(200, 80, 60, 255));
        // Grip
        FillRect(cx - 10*s, cy +  6*s,  14*s, 20*s, dark);
        // Exhaust (back)
        FillRect(cx - 68*s, cy - 10*s,  10*s, 20*s, Color.DarkGray);
    }

    private static void FillRect(float x, float y, float w, float h, Color col)
        => Raylib.DrawRectangle((int)x, (int)y, (int)Math.Max(w, 1), (int)Math.Max(h, 1), col);

    // ── Stat bars ─────────────────────────────────────────────────────────────

    private float DrawStatBars(float cx, float y, float w)
    {
        // (label, value 0..1, display text)
        var stats = new[]
        {
            ("Damage",      _weapon.Projectile.Damage / 200f,
             $"{_weapon.Projectile.Damage:F0}"),

            ("Fire Rate",   _weapon.FireRate / 30f,
             $"{_weapon.FireRate:F1} rps"),

            ("Speed",       _weapon.Projectile.Speed / 120f,
             $"{_weapon.Projectile.Speed:F0} m/s"),

            ("Recoil",      (_weapon.RecoilUp + _weapon.RecoilSide * 2f) / (15f + 10f),
             $"{_weapon.RecoilUp:F1}°"),

            ("Reload",      1f - (_weapon.ReloadTime - 0.3f) / 4.7f,
             $"{_weapon.ReloadTime:F1} s"),

            ("Impact",      _weapon.Projectile.ImpactForce / 30f,
             $"{_weapon.Projectile.ImpactForce:F0} N"),
        };

        const float barH = 14f, rowH = 22f, labelW = 70f, valW = 52f;
        float barMaxW = w - labelW - valW;

        foreach (var (label, frac, display) in stats)
        {
            float clamped = Math.Clamp(frac, 0f, 1f);
            Color fill    = StatColor(frac);

            // Label
            Raylib.DrawText(label, (int)cx, (int)(y + (rowH - UITheme.FontSizeSmall) / 2f),
                            UITheme.FontSizeSmall, UITheme.LabelPrimary);

            // Track
            Raylib.DrawRectangle((int)(cx + labelW), (int)y, (int)barMaxW, (int)barH,
                                 new Color(30, 34, 48, 255));
            // Fill
            Raylib.DrawRectangle((int)(cx + labelW), (int)y, (int)(barMaxW * clamped), (int)barH, fill);
            // Border
            Raylib.DrawRectangleLines((int)(cx + labelW), (int)y, (int)barMaxW, (int)barH,
                                      new Color(40, 44, 60, 255));

            // Value
            int vw = Raylib.MeasureText(display, UITheme.FontSizeSmall);
            Raylib.DrawText(display,
                (int)(cx + labelW + barMaxW + valW - vw),
                (int)(y + (barH - UITheme.FontSizeSmall) / 2f),
                UITheme.FontSizeSmall, UITheme.LabelSecondary);

            y += rowH;
        }

        return y;
    }

    // ── Trajectory ────────────────────────────────────────────────────────────

    private void DrawTrajectory(float cx, float y, float w, float availH)
    {
        float boxH = Math.Max(availH - 4f, 60f);
        var box = new Rectangle(cx, y, w, boxH);

        Raylib.DrawRectangleRec(box, new Color(16, 18, 28, 255));
        Raylib.DrawRectangleLinesEx(box, 1f, UITheme.PanelBorder);

        float pad    = 12f;
        float startX = box.X + pad;
        float startY = box.Y + boxH * 0.3f;   // fire height = 30% from top
        float endX   = box.X + box.Width - pad;
        float scale  = (endX - startX) / _weapon.Projectile.MaxRange;  // px per meter

        // Ground line
        float groundY = box.Y + boxH - pad;
        Raylib.DrawLine((int)startX, (int)groundY, (int)endX, (int)groundY,
                        new Color(40, 50, 40, 200));

        // Simulate trajectory
        float vx  = _weapon.Projectile.Speed;
        float vy  = 0f;
        float grav = GameConstants.GravityY * _weapon.Projectile.GravityScale;
        float dt   = 0.02f;
        float px   = 0f, py = 0f;
        float maxY = groundY - startY;   // max drop before floor

        Vector2 prev = new(startX, startY);
        bool hitGround = false;

        for (int i = 0; i < 500 && !hitGround; i++)
        {
            vy += grav * dt;
            px += vx * dt;
            py += vy * dt;

            float sx = startX + px * scale;
            float sy = startY + py * scale;

            if (sx > endX)  { sx = endX; hitGround = true; }
            if (sy > groundY) { sy = groundY; hitGround = true; }

            var cur = new Vector2(sx, sy);

            // Color fades from bright to dim as it travels
            float prog  = (sx - startX) / (endX - startX);
            byte  alpha = (byte)(255 - 180 * prog);
            Raylib.DrawLineV(prev, cur, new Color(255, 220, 80, (int)alpha));

            prev = cur;
        }

        // Range info
        float rangeM  = _weapon.Projectile.MaxRange;
        string rangeLabel = $"{rangeM:F0}m";
        Raylib.DrawText(rangeLabel,
            (int)(endX - Raylib.MeasureText(rangeLabel, UITheme.FontSizeSmall)),
            (int)(groundY - UITheme.FontSizeSmall - 2),
            UITheme.FontSizeSmall, UITheme.LabelSecondary);

        // Origin dot
        Raylib.DrawCircle((int)startX, (int)startY, 4f, Color.Yellow);

        // Gravity note
        string gravNote = _weapon.Projectile.GravityScale < 0.05f
            ? "flat trajectory"
            : $"gravity ×{_weapon.Projectile.GravityScale:F2}";
        Raylib.DrawText(gravNote,
            (int)(box.X + 6), (int)(box.Y + 6),
            UITheme.FontSizeSmall, UITheme.LabelSecondary);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float DrawSeparatorLabel(float cx, float y, float w, string label)
    {
        int tw = Raylib.MeasureText(label, UITheme.FontSizeSmall);
        int tx = (int)(cx + (w - tw) / 2f);
        int ty = (int)y;
        Raylib.DrawLine((int)cx, ty + UITheme.FontSizeSmall / 2,
                        tx - 6, ty + UITheme.FontSizeSmall / 2, UITheme.Separator);
        Raylib.DrawText(label, tx, ty, UITheme.FontSizeSmall, UITheme.LabelSecondary);
        Raylib.DrawLine(tx + tw + 6, ty + UITheme.FontSizeSmall / 2,
                        (int)(cx + w), ty + UITheme.FontSizeSmall / 2, UITheme.Separator);
        return y + UITheme.FontSizeSmall + 4f;
    }

    public static Color ClassColor(WeaponClass cls) => cls switch
    {
        WeaponClass.Pistol         => new Color(150, 180, 220, 255),
        WeaponClass.Rifle          => new Color(100, 210, 130, 255),
        WeaponClass.Shotgun        => new Color(220, 150,  60, 255),
        WeaponClass.Sniper         => new Color(100, 200, 240, 255),
        WeaponClass.RocketLauncher => new Color(220,  70,  70, 255),
        WeaponClass.Grenade        => new Color(220, 200,  60, 255),
        _                          => new Color(180, 180, 180, 255),
    };

    private static Color StatColor(float frac) => frac switch
    {
        > 0.75f => new Color(80, 210, 110, 255),
        > 0.45f => new Color(200, 200,  60, 255),
        _       => new Color(210,  70,  70, 255),
    };
}
