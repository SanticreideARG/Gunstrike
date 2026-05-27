using System.Numerics;
using GunStrike.Data;
using GunStrike.Editor.UI;
using Raylib_cs;

namespace GunStrike.Editor.Enemy;

/// <summary>
/// Enemy editor right-panel:
///   • Enemy silhouette (class-specific blocky figure)
///   • Radar chart (5 axes: Health, Speed, Accuracy, Armor, Reaction)
///   • AI state machine diagram with highlighted active behaviour
///   • Field-stats row + weapon pool
/// </summary>
public class EnemyPreview
{
    private EnemyData _enemy;

    public EnemyPreview(EnemyData enemy) { _enemy = enemy; }
    public void SetEnemy(EnemyData enemy) { _enemy = enemy; }

    // ── Draw ──────────────────────────────────────────────────────────────────

    public void Draw(Rectangle bounds)
    {
        Raylib.DrawRectangleRec(bounds, new Color(18, 20, 30, 255));

        float pad  = 20f;
        float cx   = bounds.X + pad;
        float maxW = bounds.Width - pad * 2f;
        float y    = bounds.Y + pad;

        y = DrawHeader(cx, y, maxW);
        y += 10f;

        // ── Top row: silhouette (left) + radar (right) ────────────────────────
        float topH  = 160f;
        float halfW = (maxW - 14f) / 2f;
        DrawSilhouette(cx,            y, halfW, topH);
        DrawRadar     (cx + halfW + 14f, y, halfW, topH);
        y += topH + 14f;

        // ── AI state machine ──────────────────────────────────────────────────
        y = SectionLabel(cx, y, maxW, "AI State Machine");
        y += 6f;
        DrawStateMachine(cx, y, maxW, 130f);
        y += 144f;

        // ── Field stats ───────────────────────────────────────────────────────
        y = SectionLabel(cx, y, maxW, "Field Stats");
        y += 6f;
        DrawFieldStats(cx, y, maxW);
        y += 36f;

        // ── Weapon pool ───────────────────────────────────────────────────────
        if (y + 30f < bounds.Y + bounds.Height - pad)
        {
            y = SectionLabel(cx, y, maxW, "Weapon Pool");
            y += 6f;
            DrawWeaponPool(cx, y, maxW);
        }
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private float DrawHeader(float x, float y, float w)
    {
        // Name
        Raylib.DrawText(_enemy.Name, (int)x, (int)y, 22, ClassColor(_enemy.Class));
        y += 26f;

        // Class badge + AI description
        string sub = $"{_enemy.ClassLabel}  ·  {_enemy.AI.Behavior}  ·  {(int)_enemy.Stats.MaxHealth} HP  ·  Score {_enemy.Stats.ScoreValue}";
        Raylib.DrawText(sub, (int)x, (int)y, UITheme.FontSizeSmall, UITheme.LabelSecondary);
        y += UITheme.FontSizeSmall + 2f;
        return y;
    }

    // ── Silhouette ────────────────────────────────────────────────────────────

    private void DrawSilhouette(float bx, float by, float bw, float bh)
    {
        // Box
        Raylib.DrawRectangle((int)bx, (int)by, (int)bw, (int)bh, new Color(22, 26, 38, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(bx, by, bw, bh), 1f, UITheme.PanelBorder);

        float cx = bx + bw / 2f;
        float cy = by + bh * 0.45f;   // slightly above center

        var col  = ClassColor(_enemy.Class);
        var dark = new Color((int)(col.R * 0.5f), (int)(col.G * 0.5f), (int)(col.B * 0.5f), 255);

        switch (_enemy.Class)
        {
            case EnemyClass.Grunt:     DrawGrunt    (cx, cy, 1.0f, col, dark); break;
            case EnemyClass.Heavy:     DrawHeavy    (cx, cy, 1.0f, col, dark); break;
            case EnemyClass.Sniper:    DrawSniper   (cx, cy, 1.0f, col, dark); break;
            case EnemyClass.Grenadier: DrawGrenadier(cx, cy, 1.0f, col, dark); break;
            case EnemyClass.Boss:      DrawBoss     (cx, cy, 1.0f, col, dark); break;
        }

        // HP bar below figure
        float hpFrac = Math.Clamp(_enemy.Stats.MaxHealth / 500f, 0f, 1f);
        float barX   = bx + 6f;
        float barY   = by + bh - 12f;
        float barW   = bw - 12f;
        Raylib.DrawRectangle((int)barX, (int)barY, (int)barW, 6, new Color(30, 34, 48, 255));
        Raylib.DrawRectangle((int)barX, (int)barY, (int)(barW * hpFrac), 6, col);
        Raylib.DrawRectangleLines((int)barX, (int)barY, (int)barW, 6, new Color(40, 44, 58, 255));

        int hpTw = Raylib.MeasureText($"{(int)_enemy.Stats.MaxHealth} HP", UITheme.FontSizeSmall);
        Raylib.DrawText($"{(int)_enemy.Stats.MaxHealth} HP",
            (int)(bx + bw - hpTw - 4), (int)(by + 4),
            UITheme.FontSizeSmall, UITheme.LabelSecondary);
    }

    // ── Silhouette shapes ─────────────────────────────────────────────────────

    private static void DrawGrunt(float cx, float cy, float s, Color col, Color dark)
    {
        // Head
        FR(cx - 8*s, cy - 40*s, 16*s, 15*s, col);
        // Helmet
        FR(cx - 9*s, cy - 46*s, 18*s,  8*s, dark);
        // Body
        FR(cx - 12*s, cy - 25*s, 24*s, 28*s, col);
        // Arms
        FR(cx - 20*s, cy - 24*s,  7*s, 20*s, dark);
        FR(cx + 13*s, cy - 24*s,  7*s, 20*s, dark);
        // Rifle in right hand
        FR(cx + 18*s, cy - 18*s, 22*s,  4*s, new Color(70, 65, 55, 255));
        // Legs
        FR(cx - 11*s, cy +  3*s,  9*s, 24*s, dark);
        FR(cx +  2*s, cy +  3*s,  9*s, 24*s, dark);
        // Boots
        FR(cx - 13*s, cy + 24*s, 12*s,  8*s, new Color(50, 40, 30, 255));
        FR(cx +  0*s, cy + 24*s, 12*s,  8*s, new Color(50, 40, 30, 255));
    }

    private static void DrawHeavy(float cx, float cy, float s, Color col, Color dark)
    {
        // Head (wide helmet)
        FR(cx - 11*s, cy - 38*s, 22*s, 14*s, col);
        FR(cx - 13*s, cy - 44*s, 26*s, 10*s, dark);
        // Visor slit
        FR(cx -  8*s, cy - 38*s, 16*s,  5*s, new Color(20, 80, 120, 255));
        // Wide armored body
        FR(cx - 18*s, cy - 24*s, 36*s, 30*s, col);
        // Armor plate highlights
        FR(cx - 14*s, cy - 20*s, 11*s, 10*s, dark);
        FR(cx +  3*s, cy - 20*s, 11*s, 10*s, dark);
        // Short thick arms
        FR(cx - 26*s, cy - 22*s,  7*s, 18*s, dark);
        FR(cx + 19*s, cy - 22*s,  7*s, 18*s, dark);
        // Heavy weapon
        FR(cx + 24*s, cy - 16*s, 28*s, 10*s, new Color(70, 65, 55, 255));
        FR(cx + 50*s, cy - 12*s,  6*s, 18*s, new Color(50, 45, 35, 255));
        // Short thick legs
        FR(cx - 14*s, cy +  6*s, 11*s, 18*s, dark);
        FR(cx +  3*s, cy +  6*s, 11*s, 18*s, dark);
    }

    private static void DrawSniper(float cx, float cy, float s, Color col, Color dark)
    {
        // Head (low profile)
        FR(cx - 7*s, cy - 42*s, 14*s, 13*s, col);
        FR(cx - 8*s, cy - 48*s, 16*s,  7*s, dark);
        // Thin body
        FR(cx - 8*s, cy - 29*s, 16*s, 28*s, col);
        // Thin arms extended forward
        FR(cx - 16*s, cy - 22*s,  7*s, 16*s, dark);
        FR(cx +  9*s, cy - 22*s,  7*s, 16*s, dark);
        // Long sniper rifle
        FR(cx - 32*s, cy - 20*s, 70*s,  5*s, new Color(70, 65, 55, 255));
        // Scope
        FR(cx + 4*s, cy - 26*s, 20*s,  7*s, new Color(40, 40, 55, 255));
        Raylib.DrawCircleLines((int)(cx + 14*s), (int)(cy - 23*s), 4*s, new Color(100, 200, 255, 200));
        // Bipod (if prone) — just legs + boot nubs for standing
        FR(cx - 7*s, cy -  1*s,  7*s, 22*s, dark);
        FR(cx +  0*s, cy -  1*s,  7*s, 22*s, dark);
    }

    private static void DrawGrenadier(float cx, float cy, float s, Color col, Color dark)
    {
        // Same as grunt but with grenades on belt
        DrawGrunt(cx, cy, s, col, dark);
        // Grenade icons on belt
        for (int i = -1; i <= 1; i++)
            Raylib.DrawCircle((int)(cx + i * 9 * s), (int)(cy + 3*s), 4*s,
                              new Color(70, 130, 60, 255));
        // Extra pouches
        FR(cx - 17*s, cy - 10*s, 5*s, 8*s, new Color(60, 100, 50, 255));
    }

    private static void DrawBoss(float cx, float cy, float s, Color col, Color dark)
    {
        float bs = s * 1.4f;  // boss is 40% bigger
        // Head (imposing skull-like helmet)
        FR(cx - 14*bs, cy - 44*bs, 28*bs, 18*bs, col);
        FR(cx - 16*bs, cy - 52*bs, 32*bs, 12*bs, dark);
        // Glowing visor
        FR(cx - 10*bs, cy - 44*bs, 20*bs,  8*bs, new Color(200, 50, 50, 200));
        // Massive armored body
        FR(cx - 20*bs, cy - 26*bs, 40*bs, 32*bs, col);
        FR(cx - 16*bs, cy - 22*bs, 12*bs, 12*bs, dark);
        FR(cx +  4*bs, cy - 22*bs, 12*bs, 12*bs, dark);
        // Heavy arms
        FR(cx - 28*bs, cy - 24*bs, 8*bs, 22*bs, dark);
        FR(cx + 20*bs, cy - 24*bs, 8*bs, 22*bs, dark);
        // Massive weapon
        FR(cx + 26*bs, cy - 18*bs, 36*bs, 12*bs, new Color(80, 70, 55, 255));
        FR(cx + 60*bs, cy - 14*bs,  8*bs, 20*bs, dark);
        // Legs
        FR(cx - 16*bs, cy +  6*bs, 13*bs, 24*bs, dark);
        FR(cx +  3*bs, cy +  6*bs, 13*bs, 24*bs, dark);
        // Crown/rank insignia
        for (int i = -2; i <= 2; i++)
            Raylib.DrawRectangle((int)(cx + i * 7 * bs), (int)(cy - 54*bs), (int)(4*bs), (int)(8*bs),
                                 new Color(220, 180, 40, 255));
    }

    private static void FR(float x, float y, float w, float h, Color col)
        => Raylib.DrawRectangle((int)x, (int)y, (int)Math.Max(w, 1), (int)Math.Max(h, 1), col);

    // ── Radar chart ───────────────────────────────────────────────────────────

    private void DrawRadar(float bx, float by, float bw, float bh)
    {
        Raylib.DrawRectangle((int)bx, (int)by, (int)bw, (int)bh, new Color(22, 26, 38, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(bx, by, bw, bh), 1f, UITheme.PanelBorder);

        float cx     = bx + bw / 2f;
        float cy     = by + bh / 2f + 4f;
        float radius = Math.Min(bw, bh) / 2f - 22f;   // leave room for labels
        int   n      = 5;
        float step   = MathF.PI * 2f / n;
        float base_  = -MathF.PI / 2f;   // top vertex

        string[] labels = ["Health", "Speed", "Accuracy", "Armor", "Reaction"];
        float[]  values =
        [
            Math.Clamp(_enemy.Stats.MaxHealth / 500f, 0f, 1f),
            Math.Clamp(_enemy.AI.RunSpeed     / 12f,  0f, 1f),
            Math.Clamp(1f - _enemy.AI.Inaccuracy,     0f, 1f),
            Math.Clamp(_enemy.Stats.Armor     / 100f, 0f, 1f),
            Math.Clamp(1f - _enemy.AI.ReactionTime / 2f, 0f, 1f),
        ];

        // ── Grid rings ───────────────────────────────────────────────────────
        for (float r = 0.25f; r <= 1.01f; r += 0.25f)
        {
            for (int i = 0; i < n; i++)
            {
                float a1 = base_ + i * step;
                float a2 = base_ + (i + 1) % n * step;
                Raylib.DrawLineV(
                    new Vector2(cx + MathF.Cos(a1) * radius * r, cy + MathF.Sin(a1) * radius * r),
                    new Vector2(cx + MathF.Cos(a2) * radius * r, cy + MathF.Sin(a2) * radius * r),
                    new Color(36, 40, 56, 255));
            }
        }

        // ── Axes + labels ────────────────────────────────────────────────────
        for (int i = 0; i < n; i++)
        {
            float ang = base_ + i * step;
            var tip = new Vector2(cx + MathF.Cos(ang) * radius, cy + MathF.Sin(ang) * radius);
            Raylib.DrawLineV(new Vector2(cx, cy), tip, new Color(46, 50, 68, 255));

            int   tw  = Raylib.MeasureText(labels[i], UITheme.FontSizeSmall - 2);
            float lx  = cx + MathF.Cos(ang) * (radius + 14f) - tw / 2f;
            float ly  = cy + MathF.Sin(ang) * (radius + 14f) - (UITheme.FontSizeSmall - 2) / 2f;
            Raylib.DrawText(labels[i], (int)lx, (int)ly, UITheme.FontSizeSmall - 2,
                            UITheme.LabelSecondary);
        }

        // ── Stat polygon ─────────────────────────────────────────────────────
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float ang = base_ + i * step;
            pts[i] = new Vector2(cx + MathF.Cos(ang) * radius * values[i],
                                  cy + MathF.Sin(ang) * radius * values[i]);
        }

        // Filled triangles from center (CCW winding for screen-space Y-down)
        var fill = new Color(80, 160, 220, 55);
        for (int i = 0; i < n; i++)
            Raylib.DrawTriangle(new Vector2(cx, cy), pts[(i + 1) % n], pts[i], fill);

        // Outline + vertex dots
        var lineCol = new Color(80, 170, 230, 210);
        for (int i = 0; i < n; i++)
        {
            Raylib.DrawLineV(pts[i], pts[(i + 1) % n], lineCol);
            Raylib.DrawCircle((int)pts[i].X, (int)pts[i].Y, 3f, lineCol);
        }
    }

    // ── AI state machine ──────────────────────────────────────────────────────

    private void DrawStateMachine(float bx, float by, float bw, float bh)
    {
        Raylib.DrawRectangle((int)bx, (int)by, (int)bw, (int)bh, new Color(16, 18, 28, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(bx, by, bw, bh), 1f, UITheme.PanelBorder);

        const float nw = 76f, nh = 26f;   // node width / height

        // Node X centres (as fractions of bw)
        float col0 = bx + bw * 0.10f;
        float col1 = bx + bw * 0.35f;
        float col2 = bx + bw * 0.62f;
        float col3 = bx + bw * 0.87f;

        float row0 = by + 22f;
        float row1 = by + 84f;

        // ── Node definitions ─────────────────────────────────────────────────
        // (label, cx, cy, isActive)
        bool isPatrol     = _enemy.AI.Behavior is AIBehavior.Patrol or AIBehavior.Guard;
        bool isAlert      = _enemy.AI.Behavior is AIBehavior.Guard;
        bool isAggressive = _enemy.AI.Behavior is AIBehavior.Aggressive;
        bool isCoward     = _enemy.AI.Behavior is AIBehavior.Coward;

        var nodes = new (string label, float cx, float cy, bool active)[]
        {
            ("PATROL",  col0, row0, isPatrol),
            ("ALERT",   col1, row0, isAlert),
            ("CHASE",   col2, row0, isAggressive),
            ("ATTACK",  col3, row0, isAggressive || isCoward),
            ("COVER",   col3, row1, isCoward),
            ("RETURN",  col0, row1, false),
        };

        // ── Arrows first (behind nodes) ──────────────────────────────────────
        var arrowCol = new Color(55, 60, 85, 255);

        // PATROL → ALERT
        Arrow(col0 + nw/2, row0 + nh/2,  col1 - nw/2, row0 + nh/2, arrowCol);
        // ALERT  → CHASE
        Arrow(col1 + nw/2, row0 + nh/2,  col2 - nw/2, row0 + nh/2, arrowCol);
        // CHASE  → ATTACK
        Arrow(col2 + nw/2, row0 + nh/2,  col3 - nw/2, row0 + nh/2, arrowCol);
        // ATTACK → COVER (down)
        Arrow(col3, row0 + nh,  col3, row1, arrowCol);
        // COVER  → RETURN (left)
        Arrow(col3 - nw/2, row1 + nh/2,  col0 + nw/2, row1 + nh/2, arrowCol);
        // RETURN → PATROL (up)
        Arrow(col0, row1,  col0, row0 + nh, arrowCol);

        // ── Nodes ────────────────────────────────────────────────────────────
        foreach (var (label, ncx, ncy, active) in nodes)
        {
            var r      = new Rectangle(ncx - nw/2, ncy, nw, nh);
            var col    = active ? ClassColor(_enemy.Class)
                                : new Color(32, 36, 52, 255);
            var border = active ? ClassColor(_enemy.Class)
                                : new Color(46, 50, 70, 255);

            Raylib.DrawRectangleRec(r, col);
            Raylib.DrawRectangleLinesEx(r, active ? 2f : 1f, border);

            // Glow ring for active
            if (active)
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4),
                    1f, new Color((int)ClassColor(_enemy.Class).R,
                                  (int)ClassColor(_enemy.Class).G,
                                  (int)ClassColor(_enemy.Class).B, 80));

            Color textCol = active ? Color.White : UITheme.LabelSecondary;
            int   tw      = Raylib.MeasureText(label, UITheme.FontSizeSmall);
            Raylib.DrawText(label,
                (int)(ncx - tw / 2f),
                (int)(ncy + (nh - UITheme.FontSizeSmall) / 2f),
                UITheme.FontSizeSmall, textCol);
        }

        // ── Trigger label ────────────────────────────────────────────────────
        string trigger = _enemy.AI.AlertTrigger switch
        {
            AlertTrigger.LineOfSight => "Trigger: Line-of-Sight",
            AlertTrigger.Proximity   => "Trigger: Proximity",
            AlertTrigger.Always      => "Trigger: Always Alert",
            _                        => ""
        };
        Raylib.DrawText(trigger,
            (int)(bx + bw / 2f - Raylib.MeasureText(trigger, UITheme.FontSizeSmall) / 2f),
            (int)(by + bh - UITheme.FontSizeSmall - 4),
            UITheme.FontSizeSmall, UITheme.LabelSecondary);
    }

    private static void Arrow(float x1, float y1, float x2, float y2, Color col)
    {
        Raylib.DrawLineV(new Vector2(x1, y1), new Vector2(x2, y2), col);

        // Arrowhead
        var dir  = Vector2.Normalize(new Vector2(x2 - x1, y2 - y1));
        var perp = new Vector2(-dir.Y, dir.X);
        const float sz = 5f;
        var tip  = new Vector2(x2, y2);
        var p1   = tip - dir * sz * 2 + perp * sz;
        var p2   = tip - dir * sz * 2 - perp * sz;

        // CCW winding in screen space (Y-down): tip, p2, p1
        Raylib.DrawTriangle(tip, p2, p1, col);
    }

    // ── Field stats ───────────────────────────────────────────────────────────

    private void DrawFieldStats(float cx, float y, float w)
    {
        var items = new[]
        {
            $"Sight {_enemy.AI.SightRange:F0}m",
            $"Hear {_enemy.AI.HearRange:F0}m",
            $"React {_enemy.AI.ReactionTime:F2}s",
            $"Walk {_enemy.AI.WalkSpeed:F1} m/s",
            $"Run {_enemy.AI.RunSpeed:F1} m/s",
            $"Atk range {_enemy.AI.AttackRange:F0}m",
        };

        float x  = cx;
        float colW = w / items.Length;
        foreach (var item in items)
        {
            Raylib.DrawText(item, (int)x, (int)y, UITheme.FontSizeSmall, UITheme.LabelPrimary);
            x += colW;
        }
    }

    // ── Weapon pool ───────────────────────────────────────────────────────────

    private void DrawWeaponPool(float cx, float y, float w)
    {
        if (_enemy.WeaponPool.Count == 0)
        {
            Raylib.DrawText("No weapons assigned",
                (int)cx, (int)y, UITheme.FontSizeSmall, UITheme.LabelSecondary);
            return;
        }

        float x    = cx;
        float btnW = 90f;
        foreach (var wpnId in _enemy.WeaponPool)
        {
            Raylib.DrawRectangle((int)x, (int)y, (int)btnW - 4, 22,
                                 new Color(30, 36, 52, 255));
            Raylib.DrawRectangleLines((int)x, (int)y, (int)btnW - 4, 22,
                                      new Color(46, 52, 70, 255));
            int tw = Raylib.MeasureText(wpnId, UITheme.FontSizeSmall);
            Raylib.DrawText(wpnId,
                (int)(x + (btnW - 4 - tw) / 2f),
                (int)(y + (22 - UITheme.FontSizeSmall) / 2f),
                UITheme.FontSizeSmall, UITheme.LabelAccent);
            x += btnW;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float SectionLabel(float cx, float y, float w, string label)
    {
        int tw = Raylib.MeasureText(label, UITheme.FontSizeSmall);
        int tx = (int)(cx + (w - tw) / 2f);
        int ty = (int)y;
        int my = ty + UITheme.FontSizeSmall / 2;
        Raylib.DrawLine((int)cx, my, tx - 5, my, UITheme.Separator);
        Raylib.DrawText(label, tx, ty, UITheme.FontSizeSmall, UITheme.LabelSecondary);
        Raylib.DrawLine(tx + tw + 5, my, (int)(cx + w), my, UITheme.Separator);
        return y + UITheme.FontSizeSmall + 2f;
    }

    public static Color ClassColor(EnemyClass cls) => cls switch
    {
        EnemyClass.Grunt      => new Color(100, 200, 120, 255),
        EnemyClass.Heavy      => new Color(220, 140,  50, 255),
        EnemyClass.Sniper     => new Color( 90, 190, 230, 255),
        EnemyClass.Grenadier  => new Color(190, 210,  60, 255),
        EnemyClass.Boss       => new Color(220,  60,  60, 255),
        _                     => new Color(160, 160, 160, 255),
    };
}
