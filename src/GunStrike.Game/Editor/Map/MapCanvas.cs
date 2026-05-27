using System.Numerics;
using GunStrike.Data;
using GunStrike.Editor.UI;
using Raylib_cs;

namespace GunStrike.Editor.Map;

/// <summary>
/// Grid canvas for the map editor.
/// Renders a scrollable, zoomable tile grid and handles tile placement.
/// </summary>
public class MapCanvas
{
    // ── State ─────────────────────────────────────────────────────────────────
    private MapData    _map;
    private Camera2D   _cam;
    private CanvasTool _tool   = CanvasTool.Draw;
    private TileType   _tileType = TileType.Solid;

    // ── Tile cache (X,Y) → TileData for O(1) lookup ───────────────────────────
    private readonly Dictionary<(int x, int y), TileData> _cache = [];

    // ── Pan ───────────────────────────────────────────────────────────────────
    private bool    _panning;
    private Vector2 _panMouseStart;
    private Vector2 _panTargetStart;

    // ── Hover ─────────────────────────────────────────────────────────────────
    private int _hovX = -1, _hovY = -1;

    // ── Layout ────────────────────────────────────────────────────────────────
    public const int ToolbarH = 42;

    // Most-recent bounds (set each Update/Draw call)
    private Rectangle _outerBounds;
    private bool      _firstFrame = true;

    // ── Public accessors ──────────────────────────────────────────────────────
    public CanvasTool ActiveTool { get => _tool;     set => _tool = value; }
    public TileType   TileType   { get => _tileType; set => _tileType = value; }
    public string     HoverInfo  => _hovX >= 0 ? $"Tile ({_hovX}, {_hovY})" : "";

    // ─────────────────────────────────────────────────────────────────────────

    public MapCanvas(MapData map)
    {
        _map = map;
        RebuildCache();
        _cam = new Camera2D
        {
            Target   = new Vector2(_map.WidthTiles  * _map.TileSize / 2f,
                                   _map.HeightTiles * _map.TileSize / 2f),
            Offset   = Vector2.Zero,   // set each frame
            Zoom     = 1f,
            Rotation = 0f,
        };
    }

    /// <summary>Switch to a new map and reset the tile cache.</summary>
    public void SetMap(MapData map)
    {
        _map = map;
        RebuildCache();
        _firstFrame = true;
    }

    // ── Cache helpers ─────────────────────────────────────────────────────────

    private void RebuildCache()
    {
        _cache.Clear();
        foreach (var t in _map.Tiles)
            _cache[(t.X, t.Y)] = t;
    }

    private void FlushToMap() => _map.Tiles = [.. _cache.Values];

    // ── Update ────────────────────────────────────────────────────────────────

    public void Update(Rectangle outerBounds, Vector2 mouse,
                       bool lmbDown, bool lmbPressed,
                       bool rmbDown,
                       bool mmbDown, bool mmbPressed)
    {
        _outerBounds = outerBounds;

        var canvas = CanvasBounds(outerBounds);

        // Camera offset = canvas centre (screen space)
        _cam.Offset = new Vector2(canvas.X + canvas.Width  / 2f,
                                  canvas.Y + canvas.Height / 2f);

        // Fit on first frame
        if (_firstFrame) { FitToWindow(outerBounds); _firstFrame = false; }

        bool inCanvas = Raylib.CheckCollisionPointRec(mouse, canvas);

        // ── Zoom (scroll wheel) ───────────────────────────────────────────────
        if (inCanvas)
        {
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0)
            {
                // Zoom towards cursor
                Vector2 before = Raylib.GetScreenToWorld2D(mouse, _cam);
                _cam.Zoom = Math.Clamp(_cam.Zoom * (1f + wheel * 0.12f), 0.05f, 10f);
                Vector2 after  = Raylib.GetScreenToWorld2D(mouse, _cam);
                _cam.Target   -= after - before;
            }
        }

        // ── Pan: Middle Mouse OR Space + LMB ─────────────────────────────────
        bool spaceDown = Raylib.IsKeyDown(KeyboardKey.Space);
        bool wantPan   = (_tool == CanvasTool.Pan && lmbDown)
                      || mmbDown
                      || (spaceDown && lmbDown);
        bool startPan  = (_tool == CanvasTool.Pan && lmbPressed)
                      || mmbPressed
                      || (spaceDown && lmbPressed);

        if (startPan && inCanvas)
        {
            _panning        = true;
            _panMouseStart  = mouse;
            _panTargetStart = _cam.Target;
        }
        if (!wantPan) _panning = false;

        if (_panning)
        {
            Vector2 delta = mouse - _panMouseStart;
            _cam.Target   = _panTargetStart - delta / _cam.Zoom;
        }

        // ── Hover tile ────────────────────────────────────────────────────────
        if (inCanvas && !_panning)
        {
            Vector2 world = Raylib.GetScreenToWorld2D(mouse, _cam);
            _hovX = (int)MathF.Floor(world.X / _map.TileSize);
            _hovY = (int)MathF.Floor(world.Y / _map.TileSize);
        }
        else _hovX = _hovY = -1;

        // ── Tile / Spawn / Enemy placement ────────────────────────────────────
        if (!_panning && !spaceDown && inCanvas)
        {
            switch (_tool)
            {
                case CanvasTool.Draw when lmbDown:
                    PlaceTile(_hovX, _hovY);
                    break;

                case CanvasTool.Erase when lmbDown:
                    EraseTile(_hovX, _hovY);
                    break;

                // Right-click erases in Draw mode too
                case CanvasTool.Draw when rmbDown:
                    EraseTile(_hovX, _hovY);
                    break;

                case CanvasTool.Spawn when lmbPressed:
                    PlaceSpawn(Raylib.GetScreenToWorld2D(mouse, _cam));
                    break;

                case CanvasTool.EnemyPlace when lmbPressed:
                    PlaceEnemy(Raylib.GetScreenToWorld2D(mouse, _cam));
                    break;
            }
        }
    }

    // ── Tile ops ──────────────────────────────────────────────────────────────

    private void PlaceTile(int tx, int ty)
    {
        if (!InMap(tx, ty)) return;
        _cache[(tx, ty)] = new TileData { X = tx, Y = ty, Type = _tileType };
        FlushToMap();
    }

    private void EraseTile(int tx, int ty)
    {
        if (_cache.Remove((tx, ty))) FlushToMap();
    }

    private void PlaceSpawn(Vector2 worldPos)
    {
        // Move/create the Player spawn
        var sp = _map.Spawns.Find(s => s.Name == "Player");
        if (sp == null) { sp = new SpawnPoint { Name = "Player" }; _map.Spawns.Add(sp); }
        sp.X = worldPos.X;
        sp.Y = worldPos.Y;
    }

    private void PlaceEnemy(Vector2 worldPos)
    {
        _map.Enemies.Add(new EnemyInstance { EnemyId = "generic", X = worldPos.X, Y = worldPos.Y });
    }

    private bool InMap(int tx, int ty)
        => tx >= 0 && ty >= 0 && tx < _map.WidthTiles && ty < _map.HeightTiles;

    // ── Draw ──────────────────────────────────────────────────────────────────

    public void Draw(Rectangle outerBounds)
    {
        _outerBounds = outerBounds;
        var canvas   = CanvasBounds(outerBounds);

        // ── Scissor + Camera ─────────────────────────────────────────────────
        Raylib.BeginScissorMode((int)canvas.X, (int)canvas.Y,
                                (int)canvas.Width, (int)canvas.Height);
        Raylib.BeginMode2D(_cam);

        float mapW = _map.WidthTiles  * _map.TileSize;
        float mapH = _map.HeightTiles * _map.TileSize;

        // Map background
        Raylib.DrawRectangle(0, 0, (int)mapW, (int)mapH, new Color(20, 23, 34, 255));

        // Tiles
        int ts = _map.TileSize;
        foreach (var ((tx, ty), tile) in _cache)
        {
            Raylib.DrawRectangle(tx * ts, ty * ts, ts - 1, ts - 1, TileColor(tile.Type));
        }

        // Spawns
        foreach (var sp in _map.Spawns)
        {
            Raylib.DrawCircle((int)sp.X, (int)sp.Y, 10f, new Color(50, 220, 80, 210));
            Raylib.DrawCircleLines((int)sp.X, (int)sp.Y, 10f, new Color(80, 255, 110, 255));
            Raylib.DrawText("P", (int)sp.X - 4, (int)sp.Y - 6, 12, Color.White);
        }

        // Enemy instances
        foreach (var ei in _map.Enemies)
        {
            Raylib.DrawCircle((int)ei.X, (int)ei.Y, 10f, new Color(220, 55, 55, 210));
            Raylib.DrawCircleLines((int)ei.X, (int)ei.Y, 10f, new Color(255, 80, 80, 255));
            Raylib.DrawText("E", (int)ei.X - 4, (int)ei.Y - 6, 12, Color.White);
        }

        // Grid lines
        DrawGrid(mapW, mapH);

        // Hover highlight
        if (_hovX >= 0 && _hovY >= 0 && InMap(_hovX, _hovY))
        {
            bool erasing = _tool == CanvasTool.Erase;
            Color hlFill = erasing
                ? new Color(220, 60, 60, 90)
                : new Color(120, 180, 255, 80);
            Color hlBorder = erasing
                ? new Color(220, 60, 60, 200)
                : new Color(120, 180, 255, 200);

            Raylib.DrawRectangle(_hovX * ts, _hovY * ts, ts, ts, hlFill);
            Raylib.DrawRectangleLines(_hovX * ts, _hovY * ts, ts, ts, hlBorder);
        }

        // Map border
        Raylib.DrawRectangleLines(0, 0, (int)mapW, (int)mapH, new Color(90, 100, 130, 200));

        Raylib.EndMode2D();
        Raylib.EndScissorMode();

        // Toolbar (screen space, outside scissor)
        DrawToolbar(outerBounds);
    }

    // ── Grid ──────────────────────────────────────────────────────────────────

    private void DrawGrid(float mapW, float mapH)
    {
        int ts = _map.TileSize;

        // Fine grid — only when zoomed in enough to see tiles clearly
        if (_cam.Zoom >= 0.25f)
        {
            var gc = new Color(30, 34, 48, 255);
            for (int x = 0; x <= _map.WidthTiles; x++)
                Raylib.DrawLine(x * ts, 0, x * ts, (int)mapH, gc);
            for (int y = 0; y <= _map.HeightTiles; y++)
                Raylib.DrawLine(0, y * ts, (int)mapW, y * ts, gc);
        }

        // Chunk lines (every 8 tiles) — always visible
        var cc = new Color(48, 53, 72, 255);
        for (int x = 0; x <= _map.WidthTiles; x += 8)
            Raylib.DrawLine(x * ts, 0, x * ts, (int)mapH, cc);
        for (int y = 0; y <= _map.HeightTiles; y += 8)
            Raylib.DrawLine(0, y * ts, (int)mapW, y * ts, cc);
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void DrawToolbar(Rectangle outer)
    {
        var bar = new Rectangle(outer.X, outer.Y, outer.Width, ToolbarH);
        Raylib.DrawRectangleRec(bar, UITheme.PanelBg);
        Raylib.DrawLine((int)bar.X, (int)(bar.Y + ToolbarH - 1),
                        (int)(bar.X + bar.Width), (int)(bar.Y + ToolbarH - 1),
                        UITheme.PanelBorder);

        float x   = bar.X + 8f;
        float y   = bar.Y + 5f;
        float bw  = 64f;
        float bh  = ToolbarH - 10f;
        float gap = 3f;

        // ── Tool buttons ──────────────────────────────────────────────────────
        ToolBtn(ref x, y, bw, bh, gap, "✏ Draw",    CanvasTool.Draw);
        ToolBtn(ref x, y, bw, bh, gap, "✕ Erase",   CanvasTool.Erase);
        ToolBtn(ref x, y, bw, bh, gap, "✥ Pan",     CanvasTool.Pan);
        ToolBtn(ref x, y, bw, bh, gap, "⊕ Spawn",   CanvasTool.Spawn);
        ToolBtn(ref x, y, bw, bh, gap, "★ Enemy",   CanvasTool.EnemyPlace);

        Divider(ref x, y, bh);

        // ── Tile type buttons ─────────────────────────────────────────────────
        TileBtn(ref x, y, 66f, bh, gap, "Solid",    TileType.Solid);
        TileBtn(ref x, y, 72f, bh, gap, "Platform", TileType.Platform);
        TileBtn(ref x, y, 60f, bh, gap, "Ladder",   TileType.Ladder);
        TileBtn(ref x, y, 56f, bh, gap, "Spike",    TileType.Spike);
        TileBtn(ref x, y, 56f, bh, gap, "Water",    TileType.Water);

        Divider(ref x, y, bh);

        // ── Info labels ───────────────────────────────────────────────────────
        InfoLabel(ref x, y, bh, $"Zoom: {_cam.Zoom:F2}×");
        InfoLabel(ref x, y, bh, $"Tiles: {_map.Tiles.Count}");
        if (_hovX >= 0) InfoLabel(ref x, y, bh, $"({_hovX}, {_hovY})", UITheme.LabelAccent);

        // ── Reset / Fit buttons ───────────────────────────────────────────────
        float rx = outer.X + outer.Width - 58f;
        SmallBtn(rx,      y, 26f, bh, "⌂", () => FitToWindow(_outerBounds));
        SmallBtn(rx + 28f, y, 26f, bh, "1:1", () => { _cam.Zoom = 1f; });
    }

    // ── Toolbar helpers ───────────────────────────────────────────────────────

    private void ToolBtn(ref float x, float y, float w, float h, float gap,
                         string label, CanvasTool tool)
    {
        bool active = _tool == tool;
        var rect  = new Rectangle(x, y, w, h);
        var mouse = Raylib.GetMousePosition();
        bool hov  = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hov && Raylib.IsMouseButtonPressed(MouseButton.Left))
            _tool = tool;

        Color bg     = active ? UITheme.ButtonPressed
                     : hov   ? UITheme.ButtonHover
                     :         UITheme.ButtonNormal;
        Color border = active ? UITheme.SliderFill : new Color(38, 42, 58, 255);

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1f, border);

        int tw = Raylib.MeasureText(label, UITheme.FontSizeSmall);
        Raylib.DrawText(label,
            (int)(x + (w - tw) / 2f),
            (int)(y + (h - UITheme.FontSizeSmall) / 2f),
            UITheme.FontSizeSmall,
            active ? UITheme.LabelAccent : UITheme.ButtonText);

        x += w + gap;
    }

    private void TileBtn(ref float x, float y, float w, float h, float gap,
                         string label, TileType type)
    {
        bool active = _tileType == type;
        var tileCol = TileColor(type);
        var rect    = new Rectangle(x, y, w, h);
        var mouse   = Raylib.GetMousePosition();
        bool hov    = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hov && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _tileType = type;
            if (_tool != CanvasTool.Pan) _tool = CanvasTool.Draw;
        }

        Color bg = active
            ? new Color((int)(tileCol.R * 0.7f), (int)(tileCol.G * 0.7f), (int)(tileCol.B * 0.7f), 255)
            : new Color(30, 33, 46, 255);
        if (hov && !active) bg = new Color(40, 44, 60, 255);

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1f, active ? tileCol : new Color(38, 42, 58, 255));

        // Color swatch
        Raylib.DrawRectangle((int)(x + 4), (int)(y + (h - 10) / 2f), 10, 10, tileCol);
        if (active)
            Raylib.DrawRectangleLines((int)(x + 4), (int)(y + (h - 10) / 2f), 10, 10, Color.White);

        Raylib.DrawText(label,
            (int)(x + 18),
            (int)(y + (h - UITheme.FontSizeSmall) / 2f),
            UITheme.FontSizeSmall,
            active ? Color.White : UITheme.LabelPrimary);

        x += w + gap;
    }

    private static void Divider(ref float x, float y, float h)
    {
        x += 6f;
        Raylib.DrawLine((int)x, (int)(y + 2), (int)x, (int)(y + h - 2), UITheme.Separator);
        x += 10f;
    }

    private static void InfoLabel(ref float x, float y, float h, string text,
                                   Color? col = null)
    {
        Color c = col ?? UITheme.LabelSecondary;
        Raylib.DrawText(text,
            (int)x,
            (int)(y + (h - UITheme.FontSizeSmall) / 2f),
            UITheme.FontSizeSmall, c);
        x += Raylib.MeasureText(text, UITheme.FontSizeSmall) + 14f;
    }

    private static void SmallBtn(float x, float y, float w, float h, string label, Action onClick)
    {
        var rect  = new Rectangle(x, y, w, h);
        var mouse = Raylib.GetMousePosition();
        bool hov  = Raylib.CheckCollisionPointRec(mouse, rect);

        if (hov && Raylib.IsMouseButtonPressed(MouseButton.Left))
            onClick();

        Color bg = hov ? UITheme.ButtonHover : UITheme.ButtonNormal;
        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1f, new Color(38, 42, 58, 255));

        int tw = Raylib.MeasureText(label, UITheme.FontSizeSmall);
        Raylib.DrawText(label,
            (int)(x + (w - tw) / 2f),
            (int)(y + (h - UITheme.FontSizeSmall) / 2f),
            UITheme.FontSizeSmall, UITheme.ButtonText);
    }

    // ── Camera helpers ────────────────────────────────────────────────────────

    public void FitToWindow(Rectangle outerBounds)
    {
        var canvas = CanvasBounds(outerBounds);
        float mapW = _map.WidthTiles  * _map.TileSize;
        float mapH = _map.HeightTiles * _map.TileSize;

        if (mapW <= 0 || mapH <= 0) return;

        float zx = canvas.Width  / mapW;
        float zy = canvas.Height / mapH;
        _cam.Zoom   = Math.Min(zx, zy) * 0.92f;
        _cam.Target = new Vector2(mapW / 2f, mapH / 2f);
    }

    // ── Tile colours ──────────────────────────────────────────────────────────

    public static Color TileColor(TileType t) => t switch
    {
        TileType.Solid    => new Color(72,  62,  50, 255),
        TileType.Platform => new Color(92,  80,  58, 255),
        TileType.Ladder   => new Color(185, 150,  50, 255),
        TileType.Spike    => new Color(190,  55,  55, 255),
        TileType.Water    => new Color(38,  105, 195, 255),
        _                 => new Color(80,  80,  80, 255),
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Rectangle CanvasBounds(Rectangle outer)
        => new(outer.X, outer.Y + ToolbarH, outer.Width, outer.Height - ToolbarH);
}
