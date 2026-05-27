using System.Numerics;
using Raylib_cs;

namespace GunStrike.Editor.UI;

/// <summary>
/// Single-line text input field.
/// </summary>
public class TextInput : UIElement
{
    public string          Value       { get; set; } = "";
    public string          Placeholder { get; set; } = "";
    public int             MaxLength   { get; set; } = 64;
    public Action<string>? OnChange    { get; set; }
    public Action<string>? OnSubmit    { get; set; }   // Enter key

    public bool IsFocused { get; private set; }

    private float _cursorBlink;
    private int   _cursorPos;

    public override void Update(Vector2 mouse, bool clicked, bool held, bool released)
    {
        if (!Visible || !Enabled) return;

        // Focus on click
        if (clicked)
            IsFocused = IsHovered(mouse);

        if (!IsFocused) return;

        _cursorBlink += Raylib.GetFrameTime();

        // Backspace
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _cursorPos > 0)
        {
            Value = Value[..(_cursorPos - 1)] + Value[_cursorPos..];
            _cursorPos--;
            OnChange?.Invoke(Value);
        }

        // Delete
        if (Raylib.IsKeyPressed(KeyboardKey.Delete) && _cursorPos < Value.Length)
        {
            Value = Value[.._cursorPos] + Value[(_cursorPos + 1)..];
            OnChange?.Invoke(Value);
        }

        // Arrow keys
        if (Raylib.IsKeyPressed(KeyboardKey.Left))
            _cursorPos = Math.Max(0, _cursorPos - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Right))
            _cursorPos = Math.Min(Value.Length, _cursorPos + 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Home)) _cursorPos = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.End))  _cursorPos = Value.Length;

        // Enter
        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            OnSubmit?.Invoke(Value);
            IsFocused = false;
            return;
        }

        // Escape
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            IsFocused = false;
            return;
        }

        // Character input
        int ch;
        while ((ch = Raylib.GetCharPressed()) != 0)
        {
            if (Value.Length < MaxLength && ch >= 32)
            {
                Value = Value[.._cursorPos] + (char)ch + Value[_cursorPos..];
                _cursorPos++;
                OnChange?.Invoke(Value);
            }
        }
    }

    public override void Draw()
    {
        if (!Visible) return;

        Color bg     = IsFocused ? UITheme.InputBgFocus  : UITheme.InputBg;
        Color border = IsFocused ? UITheme.InputBorderFoc : UITheme.InputBorder;

        DrawRoundedRect(Bounds, bg, border);

        // Text content
        float px = Bounds.X + 6f;
        float py = Bounds.Y + (Bounds.Height - UITheme.FontSizeNormal) / 2f;

        if (Value.Length == 0 && !IsFocused && Placeholder.Length > 0)
        {
            Raylib.DrawText(Placeholder, (int)px, (int)py,
                            UITheme.FontSizeNormal, UITheme.InputPlacehol);
        }
        else
        {
            Raylib.DrawText(Value, (int)px, (int)py,
                            UITheme.FontSizeNormal, UITheme.InputText);
        }

        // Cursor
        if (IsFocused && (int)(_cursorBlink * 2f) % 2 == 0)
        {
            string before = Value[.._cursorPos];
            int cx = (int)px + Raylib.MeasureText(before, UITheme.FontSizeNormal);
            Raylib.DrawLine(cx, (int)py, cx, (int)(py + UITheme.FontSizeNormal),
                            UITheme.InputCursor);
        }
    }
}
