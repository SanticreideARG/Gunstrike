using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// Horizontal slider.  Layout:  [Label (40%)] [track (50%)] [value (10%)]
/// </summary>
public class Slider : UIElement
{
    public string         Label        { get; set; }
    public float          Min          { get; set; }
    public float          Max          { get; set; }
    public float          Value        { get; set; }
    public string         Format       { get; set; } = "F2";
    public Action<float>? OnChange     { get; set; }

    private bool _dragging;
    private float _dragStartX;
    private float _dragStartValue;

    private const float LabelFrac = 0.42f;
    private const float ValueFrac = 0.12f;

    public Slider(string label, float min, float max, float value,
                  Action<float>? onChange = null)
    {
        Label    = label;
        Min      = min;
        Max      = max;
        Value    = value;
        OnChange = onChange;
    }

    // Track rect (the actual draggable area)
    private Rectangle TrackRect()
    {
        float lw  = Bounds.Width * LabelFrac;
        float vw  = Bounds.Width * ValueFrac;
        float tw  = Bounds.Width - lw - vw - 4;
        float pad = (Bounds.Height - 6f) / 2f;
        return new Rectangle(Bounds.X + lw + 2, Bounds.Y + pad, tw, 6f);
    }

    private float HandleX(Rectangle track)
    {
        float t = (Max == Min) ? 0f : (Value - Min) / (Max - Min);
        return track.X + t * track.Width;
    }

    public override void Update(Vector2 mouse, bool clicked, bool held, bool released)
    {
        if (!Visible || !Enabled) { _dragging = false; return; }

        var track = TrackRect();
        float hx  = HandleX(track);
        var handleRect = new Rectangle(hx - UITheme.HandleSize / 2f,
                                       Bounds.Y,
                                       UITheme.HandleSize, Bounds.Height);

        bool overHandle = Raylib.CheckCollisionPointRec(mouse, handleRect);
        bool overTrack  = Raylib.CheckCollisionPointRec(mouse, new Rectangle(
                              track.X, Bounds.Y, track.Width, Bounds.Height));

        if (clicked && (overHandle || overTrack))
        {
            _dragging      = true;
            _dragStartX    = mouse.X;
            _dragStartValue = Value;

            // Click on track (not handle): jump to that position
            if (!overHandle && overTrack)
            {
                float t = Math.Clamp((mouse.X - track.X) / track.Width, 0f, 1f);
                SetValue(Min + t * (Max - Min));
            }
        }

        if (released) _dragging = false;

        if (_dragging && held)
        {
            float dx = mouse.X - _dragStartX;
            float dt = dx / track.Width;
            SetValue(_dragStartValue + dt * (Max - Min));
        }
    }

    private void SetValue(float v)
    {
        float clamped = Math.Clamp(v, Min, Max);
        if (MathF.Abs(clamped - Value) > 0.0001f)
        {
            Value = clamped;
            OnChange?.Invoke(Value);
        }
    }

    public override void Draw()
    {
        if (!Visible) return;

        float lw     = Bounds.Width * LabelFrac;
        var track    = TrackRect();
        float hx     = HandleX(track);
        bool hovered = IsHovered(new Vector2(hx, Bounds.Y + Bounds.Height / 2f))
                    || _dragging;

        // Label
        var labelRect = new Rectangle(Bounds.X, Bounds.Y, lw, Bounds.Height);
        DrawText(Label, labelRect, UITheme.LabelPrimary, UITheme.FontSizeNormal);

        // Track bg
        Raylib.DrawRectangleRec(track, UITheme.SliderTrack);

        // Track fill (left of handle)
        float fillW = hx - track.X;
        if (fillW > 0)
            Raylib.DrawRectangle((int)track.X, (int)track.Y,
                                 (int)fillW, (int)track.Height, UITheme.SliderFill);

        // Handle
        Color hcol = (hovered || _dragging) ? UITheme.SliderHandleHv : UITheme.SliderHandle;
        float hr   = UITheme.HandleSize / 2f;
        Raylib.DrawCircle((int)hx, (int)(track.Y + track.Height / 2f), hr, hcol);
        Raylib.DrawCircleLines((int)hx, (int)(track.Y + track.Height / 2f),
                               hr, new Color(20, 20, 30, 200));

        // Value label (right side)
        float vw  = Bounds.Width * ValueFrac;
        float vx  = Bounds.X + lw + 2 + (Bounds.Width - lw - vw - 4) + 4;
        var valRect = new Rectangle(vx, Bounds.Y, vw, Bounds.Height);
        DrawText(Value.ToString(Format), valRect, UITheme.LabelAccent,
                 UITheme.FontSizeSmall);
    }
}
