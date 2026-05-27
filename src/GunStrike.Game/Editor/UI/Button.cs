using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

public enum ButtonStyle { Normal, Danger, Ghost }

public class Button : UIElement
{
    public string       Text    { get; set; }
    public Action?      OnClick { get; set; }
    public ButtonStyle  Style   { get; set; } = ButtonStyle.Normal;

    private bool _hovered;
    private bool _pressed;

    public Button(string text, Action? onClick = null)
    {
        Text    = text;
        OnClick = onClick;
    }

    public override void Update(Vector2 mouse, bool clicked, bool held, bool released)
    {
        if (!Visible || !Enabled) { _hovered = _pressed = false; return; }

        _hovered = IsHovered(mouse);
        _pressed = _hovered && held;

        if (_hovered && clicked)
            OnClick?.Invoke();
    }

    public override void Draw()
    {
        if (!Visible) return;

        Color bg = (!Enabled)  ? UITheme.ButtonDisabled
                 : (_pressed)  ? (Style == ButtonStyle.Danger ? UITheme.DangerNormal   : UITheme.ButtonPressed)
                 : (_hovered)  ? (Style == ButtonStyle.Danger ? UITheme.DangerHover     : UITheme.ButtonHover)
                 : (Style == ButtonStyle.Danger) ? UITheme.DangerNormal
                 : (Style == ButtonStyle.Ghost)  ? Color.Blank
                 :                                 UITheme.ButtonNormal;

        Color border = _hovered && Enabled
            ? new Color(120, 160, 230, 255)
            : new Color(40, 45, 60, 255);

        Color textCol = Enabled ? UITheme.ButtonText : UITheme.ButtonTextDis;

        DrawRoundedRect(Bounds, bg, border);
        DrawText(Text, Bounds, textCol, UITheme.FontSizeNormal, centerH: true);
    }
}
