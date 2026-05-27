using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// A horizontal or vertical group of mutually-exclusive radio buttons.
/// </summary>
public class RadioGroup : UIElement
{
    public string[]         Options      { get; }
    public int              Selected     { get; set; }
    public bool             Horizontal   { get; set; } = false;
    public Action<int>?     OnChange     { get; set; }

    private readonly Rectangle[] _optRects;
    private int _hovered = -1;

    public RadioGroup(string[] options, int selected = 0, Action<int>? onChange = null)
    {
        Options   = options;
        Selected  = selected;
        OnChange  = onChange;
        _optRects = new Rectangle[options.Length];
    }

    private void BuildRects()
    {
        if (Horizontal)
        {
            float w = Bounds.Width / Options.Length;
            for (int i = 0; i < Options.Length; i++)
                _optRects[i] = new Rectangle(Bounds.X + i * w, Bounds.Y, w - 2, Bounds.Height);
        }
        else
        {
            float h = UITheme.ElementHeight;
            for (int i = 0; i < Options.Length; i++)
                _optRects[i] = new Rectangle(Bounds.X, Bounds.Y + i * (h + UITheme.ElementGap),
                                             Bounds.Width, h);
        }
    }

    // Height this element needs (auto-layout support)
    public float RequiredHeight =>
        Horizontal
            ? UITheme.ElementHeight
            : Options.Length * UITheme.ElementHeight + (Options.Length - 1) * UITheme.ElementGap;

    public override void Update(Vector2 mouse, bool clicked, bool held, bool released)
    {
        if (!Visible || !Enabled) { _hovered = -1; return; }
        BuildRects();
        _hovered = -1;
        for (int i = 0; i < _optRects.Length; i++)
        {
            if (Raylib.CheckCollisionPointRec(mouse, _optRects[i]))
            {
                _hovered = i;
                if (clicked && i != Selected)
                {
                    Selected = i;
                    OnChange?.Invoke(i);
                }
            }
        }
    }

    public override void Draw()
    {
        if (!Visible) return;
        BuildRects();

        for (int i = 0; i < Options.Length; i++)
        {
            bool sel = (i == Selected);
            bool hov = (i == _hovered);

            Color bg = sel ? UITheme.RadioFill
                     : hov ? new Color(40, 45, 62, 255)
                     :       Color.Blank;

            Color border = sel ? UITheme.RadioCheck
                         : hov ? UITheme.RadioBorder
                         :       UITheme.RadioBorder;

            DrawRoundedRect(_optRects[i], bg, border);

            Color textCol = sel ? UITheme.LabelAccent : UITheme.LabelPrimary;
            DrawText(Options[i], _optRects[i], textCol,
                     UITheme.FontSizeNormal, centerH: true);
        }
    }
}
