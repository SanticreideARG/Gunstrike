using System.Numerics;
using Raylib_cs;
using GunStrike.Core;

namespace GunStrike.Rendering;

/// <summary>
/// Smooth-following camera. Wraps Raylib's Camera2D struct.
/// </summary>
public class GameCamera
{
    public Raylib_cs.Camera2D RaylibCamera { get; private set; }

    private Vector2 _target;
    private const float Smoothing = 8f;   // higher = snappier

    public GameCamera()
    {
        RaylibCamera = new Raylib_cs.Camera2D
        {
            Offset = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f),
            Target = Vector2.Zero,
            Rotation = 0f,
            Zoom = 1f
        };
    }

    public void Follow(Vector2 pixelTarget)
    {
        float dt = Raylib.GetFrameTime();
        _target = Vector2.Lerp(_target, pixelTarget, Smoothing * dt);

        var cam = RaylibCamera;
        cam.Target = _target;
        RaylibCamera = cam;
    }

    /// <summary>Convert a screen-space pixel position to world-space pixels.</summary>
    public Vector2 ScreenToWorld(Vector2 screenPos)
        => Raylib.GetScreenToWorld2D(screenPos, RaylibCamera);
}
