using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>Horizontal divider line, optionally with a label.</summary>
public class Separator : UIElement
{
    public string? Text { get; set; }

    public Separator(string? text = null) { Text = text; }

    public override void Update(Vector2 m, bool c, bool h, bool r) { }

    public override void Draw()
    {
        if (!Visible) return;
        float cy = Bounds.Y + Bounds.Height / 2f;

        if (string.IsNullOrEmpty(Text))
        {
            Raylib.DrawLine((int)Bounds.X, (int)cy,
                            (int)(Bounds.X + Bounds.Width), (int)cy,
                            UITheme.Separator);
        }
        else
        {
            int tw = Raylib.MeasureText(Text, UITheme.FontSizeSmall);
            int tx = (int)(Bounds.X + (Bounds.Width - tw) / 2f);
            int ty = (int)(cy - UITheme.FontSizeSmall / 2f);

            // Line left
            Raylib.DrawLine((int)Bounds.X, (int)cy, tx - 6, (int)cy, UITheme.Separator);
            // Label
            Raylib.DrawText(Text, tx, ty, UITheme.FontSizeSmall, UITheme.LabelSecondary);
            // Line right
            Raylib.DrawLine(tx + tw + 6, (int)cy,
                            (int)(Bounds.X + Bounds.Width), (int)cy, UITheme.Separator);
        }
    }
}
