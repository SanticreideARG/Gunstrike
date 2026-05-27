using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// Scrollable container panel.
///
/// Usage:
///   var panel = new Panel(new Rectangle(0, 0, 260, 600), "My Panel");
///   panel.Add(new Label("Hello"));
///   panel.Add(new Slider("Speed", 0, 100, 50));
///   // each frame:
///   panel.Update(mouse, clicked, held, released);
///   panel.Draw();
///
/// Auto-layout: elements are stacked top-to-bottom with ElementGap between them.
/// Horizontal splits: use AddRow(el1, el2) to place two elements side-by-side.
/// </summary>
public class Panel
{
    public Rectangle Bounds    { get; set; }
    public string?   Title     { get; set; }
    public bool      Visible   { get; set; } = true;
    public bool      CanResize { get; set; } = false;

    // ── Layout state ─────────────────────────────────────────────────────────────
    private readonly List<(UIElement el, Rectangle localRect)> _items = [];
    private float _layoutY;      // current Y cursor (relative to content origin, before scroll)
    private float _contentH;     // total content height

    // ── Scroll ───────────────────────────────────────────────────────────────────
    private float _scrollY;
    private bool  _scrollDragging;
    private float _scrollDragStart;
    private float _scrollStartY;

    // ── Content origin (below title bar, inside padding) ─────────────────────────
    private float ContentOriginY =>
        Bounds.Y + (Title != null ? UITheme.TitleBarHeight : 0) + UITheme.PanelPadding;
    private float ContentHeight  =>
        Bounds.Height - (Title != null ? UITheme.TitleBarHeight : 0) - UITheme.PanelPadding * 2;
    private float ContentWidth   =>
        Bounds.Width - UITheme.PanelPadding * 2 - UITheme.ScrollbarWidth - 4;

    // ── Public: Add elements ─────────────────────────────────────────────────────

    /// <summary>Add an element with the next auto-layout slot height.</summary>
    public T Add<T>(T el, float? height = null) where T : UIElement
    {
        float h = height ?? UITheme.ElementHeight;
        var local = new Rectangle(UITheme.PanelPadding, _layoutY, ContentWidth, h);
        _items.Add((el, local));
        _layoutY += h + UITheme.ElementGap;
        _contentH = _layoutY;
        return el;
    }

    /// <summary>Add a RadioGroup sized for its actual required height.</summary>
    public RadioGroup Add(RadioGroup rg)
    {
        float h = rg.RequiredHeight;
        var local = new Rectangle(UITheme.PanelPadding, _layoutY, ContentWidth, h);
        _items.Add((rg, local));
        _layoutY += h + UITheme.ElementGap;
        _contentH = _layoutY;
        return rg;
    }

    /// <summary>Add two elements side-by-side (50/50 split).</summary>
    public void AddRow(UIElement left, UIElement right, float? height = null)
    {
        float h  = height ?? UITheme.ElementHeight;
        float hw = (ContentWidth - UITheme.ElementGap) / 2f;

        _items.Add((left,  new Rectangle(UITheme.PanelPadding, _layoutY, hw, h)));
        _items.Add((right, new Rectangle(UITheme.PanelPadding + hw + UITheme.ElementGap, _layoutY, hw, h)));

        _layoutY += h + UITheme.ElementGap;
        _contentH = _layoutY;
    }

    /// <summary>Add two elements with custom width fractions (fractionLeft in 0..1).</summary>
    public void AddRow(UIElement left, UIElement right, float fractionLeft, float? height = null)
    {
        float h  = height ?? UITheme.ElementHeight;
        float lw = (ContentWidth - UITheme.ElementGap) * fractionLeft;
        float rw = ContentWidth - lw - UITheme.ElementGap;

        _items.Add((left,  new Rectangle(UITheme.PanelPadding, _layoutY, lw, h)));
        _items.Add((right, new Rectangle(UITheme.PanelPadding + lw + UITheme.ElementGap, _layoutY, rw, h)));

        _layoutY += h + UITheme.ElementGap;
        _contentH = _layoutY;
    }

    /// <summary>Add vertical spacing.</summary>
    public void AddSpacer(float height = 10f) => _layoutY += height;

    /// <summary>Add a Separator line.</summary>
    public void AddSeparator(string? label = null)
    {
        var sep = new Separator(label);
        Add(sep, 14f);
    }

    /// <summary>Reset the layout cursor (clears all elements).</summary>
    public void Clear()
    {
        _items.Clear();
        _layoutY = 0;
        _contentH = 0;
        _scrollY = 0;
    }

    // ── Update ───────────────────────────────────────────────────────────────────

    public void Update(Vector2 mouse, bool clicked, bool held, bool released)
    {
        if (!Visible) return;

        bool insidePanel = Raylib.CheckCollisionPointRec(mouse, Bounds);

        // Mouse wheel scroll
        if (insidePanel)
        {
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0) _scrollY = ClampScroll(_scrollY - wheel * 30f);
        }

        // Scrollbar drag
        var sbRect = ScrollbarThumbRect();
        bool overSb = sbRect.Height > 0 && Raylib.CheckCollisionPointRec(mouse, sbRect);

        if (clicked && overSb)
        {
            _scrollDragging  = true;
            _scrollDragStart = mouse.Y;
            _scrollStartY    = _scrollY;
        }
        if (released) _scrollDragging = false;

        if (_scrollDragging && held)
        {
            float trackH  = ContentHeight - UITheme.PanelPadding;
            float visible = ContentHeight;
            float total   = Math.Max(_contentH, visible);
            float ratio   = (mouse.Y - _scrollDragStart) / trackH;
            _scrollY = ClampScroll(_scrollStartY + ratio * (total - visible));
        }

        // Update children (with scroll offset applied)
        if (!insidePanel) return;

        foreach (var (el, local) in _items)
        {
            if (!el.Visible) continue;
            el.Bounds = ToAbsolute(local);
            // Only update if element is visible in the clip region
            if (el.Bounds.Y + el.Bounds.Height < ContentOriginY) continue;
            if (el.Bounds.Y > ContentOriginY + ContentHeight)    continue;
            el.Update(mouse, clicked, held, released);
        }
    }

    // ── Draw ─────────────────────────────────────────────────────────────────────

    public void Draw()
    {
        if (!Visible) return;

        // Background
        Raylib.DrawRectangleRec(Bounds, UITheme.PanelBg);
        Raylib.DrawRectangleLinesEx(Bounds, 1f, UITheme.PanelBorder);

        // Title bar
        if (Title != null)
        {
            var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, UITheme.TitleBarHeight);
            Raylib.DrawRectangleRec(titleBar, UITheme.PanelTitle);
            Raylib.DrawLine((int)Bounds.X, (int)(Bounds.Y + UITheme.TitleBarHeight),
                            (int)(Bounds.X + Bounds.Width),
                            (int)(Bounds.Y + UITheme.TitleBarHeight),
                            UITheme.PanelBorder);
            Raylib.DrawText(Title,
                            (int)(Bounds.X + UITheme.PanelPadding),
                            (int)(Bounds.Y + (UITheme.TitleBarHeight - UITheme.FontSizeMedium) / 2f),
                            UITheme.FontSizeMedium, UITheme.PanelTitleText);
        }

        // Clip to content area
        int cx = (int)Bounds.X;
        int cy = (int)ContentOriginY;
        int cw = (int)(Bounds.Width - UITheme.ScrollbarWidth - 2);
        int ch = (int)ContentHeight;
        Raylib.BeginScissorMode(cx, cy, cw, ch);

        foreach (var (el, local) in _items)
        {
            if (!el.Visible) continue;
            el.Bounds = ToAbsolute(local);
            if (el.Bounds.Y + el.Bounds.Height < cy) continue;
            if (el.Bounds.Y > cy + ch)               continue;
            el.Draw();
        }

        Raylib.EndScissorMode();

        // Scrollbar
        DrawScrollbar();
    }

    // ── Scrollbar ────────────────────────────────────────────────────────────────

    private void DrawScrollbar()
    {
        float visible = ContentHeight;
        float total   = Math.Max(_contentH, visible);
        if (total <= visible) return;

        float sbX = Bounds.X + Bounds.Width - UITheme.ScrollbarWidth - 1;
        float sbY = ContentOriginY;
        float sbH = ContentHeight;

        Raylib.DrawRectangle((int)sbX, (int)sbY,
                             (int)UITheme.ScrollbarWidth, (int)sbH,
                             UITheme.ScrollTrack);

        var thumb = ScrollbarThumbRect();
        bool hov  = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), thumb);
        Raylib.DrawRectangleRec(thumb, hov || _scrollDragging
            ? UITheme.ScrollThumbHv : UITheme.ScrollThumb);
    }

    private Rectangle ScrollbarThumbRect()
    {
        float visible = ContentHeight;
        float total   = Math.Max(_contentH, visible);
        if (total <= visible) return default;

        float sbX = Bounds.X + Bounds.Width - UITheme.ScrollbarWidth - 1;
        float sbY = ContentOriginY;
        float sbH = ContentHeight;

        float thumbH  = Math.Max(30f, sbH * (visible / total));
        float thumbY  = sbY + (_scrollY / (total - visible)) * (sbH - thumbH);

        return new Rectangle(sbX, thumbY, UITheme.ScrollbarWidth, thumbH);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private Rectangle ToAbsolute(Rectangle local)
        => new(Bounds.X + local.X,
               ContentOriginY + local.Y - _scrollY,
               local.Width, local.Height);

    private float ClampScroll(float v)
    {
        float maxScroll = Math.Max(0f, _contentH - ContentHeight);
        return Math.Clamp(v, 0f, maxScroll);
    }
}
