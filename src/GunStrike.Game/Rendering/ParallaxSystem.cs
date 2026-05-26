using GunStrike.Core;

namespace GunStrike.Rendering;

/// <summary>
/// Manages the 3 parallax planes.
///
///  Plane 1 — Sky       : scrollFactor = 0.04  (20% de Plano 2)
///  Plane 2 — Distant   : scrollFactor = 0.20  (20% de Plano 3)
///  Plane 3 — Map       : scrollFactor = 1.00  (normal)
///
/// The map layer (Plane 3) is the physics world; its "scroll" is the camera itself.
/// Planes 1 and 2 are pure visuals drawn before the map.
/// </summary>
public class ParallaxSystem
{
    private readonly ParallaxLayer _sky;
    private readonly ParallaxLayer _mid;

    public ParallaxSystem(MapEnvironment env)
    {
        _sky = new ParallaxLayer(LayerTheme.Sky, GameConstants.ParallaxSky);

        var midTheme = env switch
        {
            MapEnvironment.Desert => LayerTheme.Dunes,
            MapEnvironment.Urban  => LayerTheme.Buildings,
            _                     => LayerTheme.Mountains,
        };
        _mid = new ParallaxLayer(midTheme, GameConstants.ParallaxMid);
    }

    /// <summary>
    /// Draw planes 1 and 2. Must be called BEFORE BeginMode2D so they render
    /// in screen space (no camera transform applied).
    /// cameraWorldX: left edge of the camera in world pixels.
    /// </summary>
    public void DrawBackground(float cameraWorldX)
    {
        _sky.Draw(cameraWorldX);
        _mid.Draw(cameraWorldX);
    }
}

/// <summary>
/// Determines which mid-ground theme is used.
/// </summary>
public enum MapEnvironment
{
    Forest,
    Desert,
    Urban,
}
