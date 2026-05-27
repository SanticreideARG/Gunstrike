using System.Numerics;
using Raylib_cs;
using GunStrike.Core;

namespace GunStrike.Rendering;

/// <summary>
/// A single body-part sprite generated procedurally as a RenderTexture.
/// Drawn with DrawTexturePro so it can be:
///   - Positioned at world pixel coords
///   - Rotated around its pivot point
///   - Flipped horizontally (facing direction)
/// </summary>
public class CharacterSprite : IDisposable
{
    public  RenderTexture2D Texture  { get; private set; }
    public  int             Width    => Texture.Texture.Width;
    public  int             Height   => Texture.Texture.Height;

    /// <summary>
    /// Pivot offset from the sprite's top-left corner (pixels).
    /// This is the joint anchor — the point the sprite rotates around.
    /// </summary>
    public Vector2 Pivot { get; }

    private bool _disposed;

    public CharacterSprite(int width, int height, Vector2 pivot,
                           Action<int, int> drawContent)
    {
        Pivot   = pivot;
        Texture = Raylib.LoadRenderTexture(width, height);

        Raylib.BeginTextureMode(Texture);
        Raylib.ClearBackground(Color.Blank);
        drawContent(width, height);
        Raylib.EndTextureMode();
    }

    /// <summary>
    /// Draw the sprite.
    /// worldPos  : center of the physics body in world pixels.
    /// angleDeg  : rotation in degrees (physics body rotation).
    /// facingRight: when false, flip horizontally.
    /// tint      : color tint (Color.White = no tint).
    /// </summary>
    public void Draw(Vector2 worldPos, float angleDeg, bool facingRight,
                     Color? tint = null)
    {
        float scaleX = facingRight ? 1f : -1f;

        // Source: full texture (Y-flipped because RenderTexture is upside down in Raylib)
        var src = new Rectangle(0, 0, Width * scaleX, -Height);

        // Dest: centered on worldPos, sized naturally
        var dst = new Rectangle(worldPos.X, worldPos.Y, Width, Height);

        // Origin: pivot point (joint anchor) within the sprite
        var origin = facingRight ? Pivot : new Vector2(Width - Pivot.X, Pivot.Y);

        Raylib.DrawTexturePro(Texture.Texture, src, dst, origin,
                              angleDeg, tint ?? Color.White);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Raylib.UnloadRenderTexture(Texture);
        _disposed = true;
    }
}
