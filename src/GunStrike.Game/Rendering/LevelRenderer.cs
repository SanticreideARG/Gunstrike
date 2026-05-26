using System.Numerics;
using Raylib_cs;
using GunStrike.Core;
using GunStrike.Physics;

namespace GunStrike.Rendering;

/// <summary>
/// Plano 3 — mapa físico con suelo y plataformas.
/// Hardcoded para el nivel de prueba. Se reemplazará con carga desde JSON/tilemap.
///
/// Escala: PhysicsToPixels = 200  →  1 m = 200 px
/// Personaje ≈ 275 px  →  ~1.375 m
/// </summary>
public class LevelRenderer
{
    private readonly PhysicsWorld _physics;

    // (centerX, centerY, width, height) en metros
    // Nivel de prueba: suelo ancho + plataformas escalonadas
    private static readonly (float cx, float cy, float w, float h, Color color)[] Tiles =
    [
        // === Suelo principal ===
        (20f,  14.5f,  60f,  1.0f, new Color(70, 100, 65, 255)),

        // === Plataformas ===
        ( 5f,  10.5f,  4.0f,  0.4f, new Color(90, 120, 80, 255)),
        (10f,   9.0f,  3.5f,  0.4f, new Color(90, 120, 80, 255)),
        (15f,   7.5f,  4.0f,  0.4f, new Color(90, 120, 80, 255)),
        (22f,   8.5f,  5.0f,  0.4f, new Color(90, 120, 80, 255)),
        (29f,   6.5f,  3.5f,  0.4f, new Color(90, 120, 80, 255)),
        (35f,   9.0f,  4.0f,  0.4f, new Color(90, 120, 80, 255)),

        // === Paredes (bordes del nivel) ===
        ( 0.5f,  7.5f,  1.0f, 15.0f, new Color(55, 75, 50, 255)),
        (39.5f,  7.5f,  1.0f, 15.0f, new Color(55, 75, 50, 255)),
    ];

    public LevelRenderer(PhysicsWorld physics) => _physics = physics;

    public void Load()
    {
        foreach (var (cx, cy, w, h, _) in Tiles)
            _physics.CreateStaticBox(new Vector2(cx, cy), w, h);
    }

    public void Draw()
    {
        foreach (var (cx, cy, w, h, color) in Tiles)
        {
            int px = (int)((cx - w / 2f) * GameConstants.PhysicsToPixels);
            int py = (int)((cy - h / 2f) * GameConstants.PhysicsToPixels);
            int pw = (int)(w * GameConstants.PhysicsToPixels);
            int ph = (int)(h * GameConstants.PhysicsToPixels);

            Raylib.DrawRectangle(px, py, pw, ph, color);

            // Borde top highlight
            Raylib.DrawRectangle(px, py, pw, 3,
                new Color(Math.Min(color.R + 40, 255),
                          Math.Min(color.G + 40, 255),
                          Math.Min(color.B + 40, 255), 255));

            Raylib.DrawRectangleLines(px, py, pw, ph,
                new Color(Math.Min(color.R + 20, 255),
                          Math.Min(color.G + 20, 255),
                          Math.Min(color.B + 20, 255), 180));
        }
    }

    public void Unload() { }
}
