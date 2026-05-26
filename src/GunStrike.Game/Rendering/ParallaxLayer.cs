using System.Numerics;
using Raylib_cs;
using GunStrike.Core;

namespace GunStrike.Rendering;

/// <summary>
/// Which visual theme a parallax layer uses.
/// </summary>
public enum LayerTheme
{
    Sky,          // Plano 1: cielo — degradado, nubes procedurales
    Mountains,    // Plano 2 opción A: montañas
    Dunes,        // Plano 2 opción B: dunas del desierto
    Buildings,    // Plano 2 opción C: edificios urbanos
    UrbanMap,     // Plano 3: ciudad industrial (fondo del mapa)
    DesertMap,    // Plano 3: desierto
}

/// <summary>
/// A single parallax background layer.
/// Draws procedurally (no textures yet) and tiles horizontally.
/// </summary>
public class ParallaxLayer
{
    public float       ScrollFactor { get; }
    public LayerTheme  Theme        { get; }

    // Width of one tile in pixels (layers repeat horizontally)
    private const int TileWidth = 1280;

    public ParallaxLayer(LayerTheme theme, float scrollFactor)
    {
        Theme        = theme;
        ScrollFactor = scrollFactor;
    }

    /// <summary>
    /// Draw the layer.
    /// cameraX: left edge of the camera in world pixels (unscaled, full map coords).
    /// </summary>
    public void Draw(float cameraX)
    {
        // How much this layer has scrolled
        float scrolledX = cameraX * ScrollFactor;

        // Offset within the tile (for seamless tiling)
        float tileOffset = scrolledX % TileWidth;

        // We draw enough tiles to fill the screen (2 tiles covers any offset)
        for (int i = -1; i <= 1; i++)
        {
            float drawX = -tileOffset + i * TileWidth;
            DrawTile(drawX);
        }
    }

    private void DrawTile(float x)
    {
        int ix = (int)x;
        int w  = TileWidth;
        int h  = GameConstants.ScreenHeight;

        switch (Theme)
        {
            case LayerTheme.Sky:        DrawSky(ix, w, h);       break;
            case LayerTheme.Mountains:  DrawMountains(ix, w, h);  break;
            case LayerTheme.Dunes:      DrawDunes(ix, w, h);      break;
            case LayerTheme.Buildings:  DrawBuildings(ix, w, h);  break;
            case LayerTheme.UrbanMap:   DrawUrbanMapBg(ix, w, h); break;
            case LayerTheme.DesertMap:  DrawDesertMapBg(ix, w, h);break;
        }
    }

    // ── Sky ─────────────────────────────────────────────────────────────────────
    private static void DrawSky(int x, int w, int h)
    {
        // Vertical gradient: deep blue top → lighter horizon
        int bands = 12;
        for (int b = 0; b < bands; b++)
        {
            float t = b / (float)bands;
            byte r = (byte)Lerp(25,  120, t);
            byte g = (byte)Lerp(45,  165, t);
            byte bv= (byte)Lerp(110, 210, t);
            Raylib.DrawRectangle(x, (int)(h * t / 1f), w, h / bands + 2, new Color((int)r, (int)g, (int)bv, 255));
        }

        // Simple cloud shapes (deterministic positions based on tile X)
        DrawClouds(x, w, h);
    }

    private static void DrawClouds(int tileX, int w, int h)
    {
        // Deterministic clouds per tile using a simple hash
        var rng = new System.Random(tileX / TileWidth + 42);
        int count = rng.Next(3, 7);
        for (int i = 0; i < count; i++)
        {
            int cx = tileX + rng.Next(60, w - 60);
            int cy = rng.Next(30, h / 3);
            int cw = rng.Next(80, 220);
            int ch = cw / 3;
            // Main ellipse
            Raylib.DrawEllipse(cx, cy, cw / 2f, ch / 2f, new Color(230, 235, 245, 200));
            // Puff left
            Raylib.DrawEllipse(cx - cw / 4, cy + ch / 6, cw / 3f, ch / 2.5f, new Color(235, 240, 250, 180));
            // Puff right
            Raylib.DrawEllipse(cx + cw / 4, cy + ch / 6, cw / 3f, ch / 2.5f, new Color(235, 240, 250, 180));
        }
    }

    // ── Mountains ───────────────────────────────────────────────────────────────
    private static void DrawMountains(int x, int w, int h)
    {
        // Background mountain range (darker, farther)
        DrawMountainRange(x, w, h,
            seed:      7,
            peakCount: 5,
            minH: 0.30f, maxH: 0.65f,
            color: new Color(55, 65, 85, 255));

        // Foreground range (lighter, closer)
        DrawMountainRange(x, w, h,
            seed:      13,
            peakCount: 7,
            minH: 0.15f, maxH: 0.45f,
            color: new Color(80, 95, 115, 255));

        // Snow caps
        DrawMountainRange(x, w, h,
            seed:      7,
            peakCount: 5,
            minH: 0.50f, maxH: 0.65f,
            color: new Color(220, 225, 235, 200));
    }

    private static void DrawMountainRange(int x, int w, int h,
        int seed, int peakCount, float minH, float maxH, Color color)
    {
        var rng     = new System.Random(seed + x / TileWidth * 31);
        var points  = new List<Vector2>();
        int stepX   = w / peakCount;

        points.Add(new Vector2(x, h));
        for (int i = 0; i <= peakCount; i++)
        {
            float px  = x + i * stepX;
            float py  = h - h * ((float)rng.NextDouble() * (maxH - minH) + minH);
            points.Add(new Vector2(px, py));
        }
        points.Add(new Vector2(x + w, h));

        // Draw as filled triangles (fan from bottom)
        for (int i = 1; i < points.Count - 1; i++)
            Raylib.DrawTriangle(points[i + 1], points[i], points[0], color);
    }

    // ── Dunes ────────────────────────────────────────────────────────────────────
    private static void DrawDunes(int x, int w, int h)
    {
        // Sky portion (sandy gradient top → orange horizon)
        int skyH = (int)(h * 0.55f);
        for (int b = 0; b < 8; b++)
        {
            float t = b / 8f;
            byte r = (byte)Lerp(120, 210, t);
            byte g = (byte)Lerp(130, 145, t);
            byte bv= (byte)Lerp(160,  80, t);
            Raylib.DrawRectangle(x, (int)(skyH * t / 1f), w, skyH / 8 + 2, new Color((int)r, (int)g, (int)bv, 255));
        }

        // Back dune row
        DrawDuneRow(x, w, h, seed: 5,  heightFrac: 0.40f, color: new Color(190, 155,  95, 255));
        // Mid dune row
        DrawDuneRow(x, w, h, seed: 11, heightFrac: 0.28f, color: new Color(205, 170, 110, 255));
        // Front dune edge
        DrawDuneRow(x, w, h, seed: 17, heightFrac: 0.18f, color: new Color(220, 185, 125, 255));
    }

    private static void DrawDuneRow(int x, int w, int h,
        int seed, float heightFrac, Color color)
    {
        var rng    = new System.Random(seed + x / TileWidth * 17);
        int baseY  = (int)(h * (1f - heightFrac));
        int points = 8;
        int step   = w / points;

        var poly = new List<Vector2> { new(x, h) };
        for (int i = 0; i <= points; i++)
        {
            float px = x + i * step;
            float sine = MathF.Sin(i * 1.4f + seed) * (h * heightFrac * 0.3f);
            float py = baseY + sine + (float)rng.NextDouble() * 20 - 10;
            poly.Add(new Vector2(px, py));
        }
        poly.Add(new Vector2(x + w, h));

        for (int i = 1; i < poly.Count - 1; i++)
            Raylib.DrawTriangle(poly[i + 1], poly[i], poly[0], color);
    }

    // ── Buildings (urban silhouette) ─────────────────────────────────────────────
    private static void DrawBuildings(int x, int w, int h)
    {
        // Night sky gradient
        for (int b = 0; b < 10; b++)
        {
            float t = b / 10f;
            byte r = (byte)Lerp(10, 35, t);
            byte g = (byte)Lerp(10, 40, t);
            byte bv= (byte)Lerp(30, 75, t);
            Raylib.DrawRectangle(x, (int)(h * t / 1f), w, h / 10 + 2, new Color((int)r, (int)g, (int)bv, 255));
        }

        // Back buildings (dark)
        DrawBuildingRow(x, w, h, seed: 3,  minH: 0.25f, maxH: 0.65f,
            minW: 40, maxW: 90, color: new Color(25, 30, 45, 255));
        // Front buildings (slightly lighter)
        DrawBuildingRow(x, w, h, seed: 9,  minH: 0.20f, maxH: 0.50f,
            minW: 30, maxW: 70, color: new Color(35, 42, 62, 255));

        // A few lit windows
        DrawWindows(x, w, h, seed: 3, minBH: 0.25f, maxBH: 0.65f);
    }

    private static void DrawBuildingRow(int x, int w, int h,
        int seed, float minH, float maxH, int minW, int maxW, Color color)
    {
        var rng  = new System.Random(seed + x / TileWidth * 23);
        int curX = x;
        while (curX < x + w)
        {
            int bw = rng.Next(minW, maxW);
            int bh = (int)(h * ((float)rng.NextDouble() * (maxH - minH) + minH));
            int by = h - bh;
            Raylib.DrawRectangle(curX, by, bw, bh, color);
            curX += bw + rng.Next(2, 12);
        }
    }

    private static void DrawWindows(int x, int w, int h,
        int seed, float minBH, float maxBH)
    {
        var rng  = new System.Random(seed + x / TileWidth * 23 + 100);
        int curX = x;
        while (curX < x + w)
        {
            int bw  = rng.Next(40, 90);
            int bh  = (int)(h * ((float)rng.NextDouble() * (maxBH - minBH) + minBH));
            int by  = h - bh;
            int rows = bh / 18, cols = bw / 12;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                if (rng.Next(4) == 0)  // 25% lit
                {
                    byte br = (byte)rng.Next(180, 255);
                    byte bg = (byte)rng.Next(160, 230);
                    Raylib.DrawRectangle(curX + col * 12 + 2, by + row * 18 + 3, 6, 10,
                        new Color((int)br, (int)bg, 100, 200));
                }
            }
            curX += bw + rng.Next(2, 12);
        }
    }

    // ── Map background fillers (Plano 3 — atmospheric depth) ───────────────────
    private static void DrawUrbanMapBg(int x, int w, int h)
    {
        // Dark concrete gradient
        Raylib.DrawRectangle(x, 0, w, h, new Color(45, 48, 55, 255));
    }

    private static void DrawDesertMapBg(int x, int w, int h)
    {
        Raylib.DrawRectangle(x, 0, w, h, new Color(180, 155, 100, 255));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
