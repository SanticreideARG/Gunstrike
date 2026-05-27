using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// Base class for all editor UI widgets.
/// Position is in ABSOLUTE screen pixels (no transform).
/// Scroll offset is applied by the parent Panel before calling Update/Draw.
/// </summary>
public abstract class UIElement
{
    public Rectangle Bounds  { get; set; }
    public bool      Visible { get; set; } = true;
    public bool      Enabled { get; set; } = true;
    public string?   Tooltip { get; set; }

    /// <summary>
    /// Process input. Called each frame by the owning Panel.
    /// mouse      : absolute cursor position in screen pixels.
    /// clicked    : mouse button was pressed THIS frame.
    /// held       : mouse button is currently held down.
    /// released   : mouse button was released THIS frame.
    /// </summary>
    public abstract void Update(Vector2 mouse, bool clicked, bool held, bool released);

    /// <summary>Draw the widget in screen space.</summary>
    public abstract void Draw();

    // ── Helpers ──────────────────────────────────────────────────────────────────

    protected bool IsHovered(Vector2 mouse)
        => Enabled && Visible && Raylib.CheckCollisionPointRec(mouse, Bounds);

    protected static void DrawRoundedRect(Rectangle r, Color fill, Color border,
                                           float radius = UITheme.CornerRadius)
    {
        Raylib.DrawRectangleRounded(r, radius / Math.Max(r.Width, r.Height), 4, fill);
        Raylib.DrawRectangleRoundedLines(r, radius / Math.Max(r.Width, r.Height), 4, 1f, border);
    }

    protected static void DrawText(string text, Rectangle bounds,
                                    Color color, int fontSize = UITheme.FontSizeNormal,
                                    bool centerH = false, bool centerV = true)
    {
        int tw = Raylib.MeasureText(text, fontSize);
        float x = centerH
            ? bounds.X + (bounds.Width  - tw) / 2f
            : bounds.X + UITheme.PanelPadding / 2f;
        float y = centerV
            ? bounds.Y + (bounds.Height - fontSize) / 2f
            : bounds.Y;
        Raylib.DrawText(text, (int)x, (int)y, fontSize, color);
    }
}
