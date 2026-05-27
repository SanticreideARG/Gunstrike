using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

public enum LabelStyle { Primary, Secondary, Accent, Header }

public class Label : UIElement
{
    public string      Text     { get; set; }
    public LabelStyle  Style    { get; set; } = LabelStyle.Primary;
    public bool        CenterH  { get; set; } = false;

    public Label(string text, LabelStyle style = LabelStyle.Primary)
    {
        Text  = text;
        Style = style;
    }

    public override void Update(Vector2 mouse, bool clicked, bool held, bool released) { }

    public override void Draw()
    {
        if (!Visible) return;

        int fontSize = Style == LabelStyle.Header
            ? UITheme.FontSizeMedium
            : UITheme.FontSizeNormal;

        Color col = Style switch
        {
            LabelStyle.Secondary => UITheme.LabelSecondary,
            LabelStyle.Accent    => UITheme.LabelAccent,
            LabelStyle.Header    => UITheme.PanelTitleText,
            _                    => UITheme.LabelPrimary,
        };

        DrawText(Text, Bounds, col, fontSize, CenterH);
    }
}
