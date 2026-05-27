using System.Numerics;
using GunStrike.Data;
using GunStrike.Editor.UI;
using Raylib_cs;

namespace GunStrike.Editor;

/// <summary>
/// Stand-alone editor window.
/// Launch via: dotnet run -- --editor map | weapon | enemy
/// </summary>
public class EditorApp
{
    // ── Window ────────────────────────────────────────────────────────────────
    private const int WinW = 1280;
    private const int WinH = 720;
    private const int PanelW = 280;

    // ── Mode ──────────────────────────────────────────────────────────────────
    private EditorMode _mode;

    // ── Data ──────────────────────────────────────────────────────────────────
    private MapData    _map    = DataSerializer.DefaultMap();
    private WeaponData _weapon = DataSerializer.DefaultWeapon();
    private EnemyData  _enemy  = DataSerializer.DefaultEnemy();

    // ── UI Panels ─────────────────────────────────────────────────────────────
    private Panel _sidePanel = null!;
    private Panel _modeBar   = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private string _statusMsg  = "Ready.";
    private double _statusTime = 0;

    // ── Data dir ──────────────────────────────────────────────────────────────
    private readonly string _dataDir;

    public EditorApp(EditorMode mode, string? dataDir = null)
    {
        _mode    = mode;
        _dataDir = dataDir ?? Path.Combine(AppContext.BaseDirectory, "data");
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(WinW, WinH, $"GunStrike Editor — {_mode}");
        Raylib.SetTargetFPS(60);

        BuildPanels();

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        Raylib.CloseWindow();
    }

    // ── Build / rebuild panels ────────────────────────────────────────────────

    private void BuildPanels()
    {
        // Mode bar (top-left, fixed height)
        _modeBar = new Panel
        {
            Bounds = new Rectangle(0, 0, PanelW, 60),
        };
        var modeGroup = _modeBar.Add(new RadioGroup(
            ["Map", "Weapon", "Enemy"],
            (int)_mode,
            i => { _mode = (EditorMode)i; RebuildSidePanel(); }),
            height: 36f);
        modeGroup.Horizontal = true;

        // Side panel
        _sidePanel = new Panel
        {
            Bounds = new Rectangle(0, 64, PanelW, Raylib.GetScreenHeight() - 64 - 24),
            Title  = ModeTitle(),
        };
        PopulateSidePanel();
    }

    private void RebuildSidePanel()
    {
        _sidePanel.Title = ModeTitle();
        _sidePanel.Clear();
        PopulateSidePanel();
    }

    private string ModeTitle() => _mode switch
    {
        EditorMode.Map    => "Map Properties",
        EditorMode.Weapon => "Weapon Properties",
        EditorMode.Enemy  => "Enemy Properties",
        _                 => "Editor"
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Populate the side panel with controls for the active mode
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateSidePanel()
    {
        switch (_mode)
        {
            case EditorMode.Map:    BuildMapPanel();    break;
            case EditorMode.Weapon: BuildWeaponPanel(); break;
            case EditorMode.Enemy:  BuildEnemyPanel();  break;
        }

        _sidePanel.AddSpacer(8f);
        _sidePanel.AddSeparator();

        // Save / Load buttons
        var saveBtn = _sidePanel.Add(new Button("💾  Save", () => SaveCurrent()));
        saveBtn.Style = ButtonStyle.Normal;

        var loadBtn = _sidePanel.Add(new Button("📂  Load…", () => LoadCurrent()));
        loadBtn.Style = ButtonStyle.Ghost;

        var newBtn = _sidePanel.Add(new Button("✦  New", () => NewCurrent()));
        newBtn.Style = ButtonStyle.Ghost;
    }

    // ── Map panel ─────────────────────────────────────────────────────────────

    private void BuildMapPanel()
    {
        _sidePanel.Add(new Label("Name", LabelStyle.Secondary));
        var nameInput = _sidePanel.Add(new TextInput { Value = _map.Name, Placeholder = "map name" });
        nameInput.OnChange = v => _map.Name = v;

        _sidePanel.AddSeparator("Environment");
        _sidePanel.Add(new RadioGroup(
            ["Mountains", "Dunes", "Urban"],
            (int)_map.Environment,
            i => _map.Environment = (MapEnvironment)i));

        _sidePanel.AddSeparator("Size");
        var wSlider = _sidePanel.Add(new Slider("Width (tiles)", 20, 200, _map.WidthTiles,
            v => _map.WidthTiles = (int)v) { Format = "F0" });
        var hSlider = _sidePanel.Add(new Slider("Height (tiles)", 10, 60,  _map.HeightTiles,
            v => _map.HeightTiles = (int)v) { Format = "F0" });
        var tsSlider = _sidePanel.Add(new Slider("Tile size (px)", 16, 64, _map.TileSize,
            v => _map.TileSize = (int)v) { Format = "F0" });

        _sidePanel.AddSeparator("Info");
        _sidePanel.Add(new Label($"Tiles: {_map.Tiles.Count}", LabelStyle.Secondary));
        _sidePanel.Add(new Label($"Spawns: {_map.Spawns.Count}", LabelStyle.Secondary));
        _sidePanel.Add(new Label($"Enemies: {_map.Enemies.Count}", LabelStyle.Secondary));
    }

    // ── Weapon panel ──────────────────────────────────────────────────────────

    private void BuildWeaponPanel()
    {
        _sidePanel.Add(new Label("Name", LabelStyle.Secondary));
        var nameInput = _sidePanel.Add(new TextInput { Value = _weapon.Name, Placeholder = "weapon name" });
        nameInput.OnChange = v => _weapon.Name = v;

        _sidePanel.AddSeparator("Class");
        _sidePanel.Add(new RadioGroup(
            ["Pistol", "Rifle", "Shotgun", "Sniper", "RPG"],
            (int)_weapon.Class,
            i => _weapon.Class = (WeaponClass)i));

        _sidePanel.AddSeparator("Fire Mode");
        _sidePanel.Add(new RadioGroup(
            ["Semi", "Auto", "Burst"],
            (int)_weapon.FireMode,
            i => _weapon.FireMode = (FireMode)i));

        _sidePanel.AddSeparator("Shooting");
        _sidePanel.Add(new Slider("Fire Rate",    1f,   30f,  _weapon.FireRate,
            v => _weapon.FireRate = v));
        _sidePanel.Add(new Slider("Spread (°)",   0f,   20f,  _weapon.Spread,
            v => _weapon.Spread = v));
        _sidePanel.Add(new Slider("Mag Size",      1f,  100f,  _weapon.MagSize,
            v => _weapon.MagSize = (int)v) { Format = "F0" });
        _sidePanel.Add(new Slider("Reload (s)",   0.3f,  5f,  _weapon.ReloadTime));

        _sidePanel.AddSeparator("Projectile");
        _sidePanel.Add(new Slider("Speed (m/s)",  10f, 120f,  _weapon.Projectile.Speed,
            v => _weapon.Projectile.Speed = v));
        _sidePanel.Add(new Slider("Gravity",       0f,   1f,  _weapon.Projectile.GravityScale,
            v => _weapon.Projectile.GravityScale = v));
        _sidePanel.Add(new Slider("Damage",        1f, 200f,  _weapon.Projectile.Damage,
            v => _weapon.Projectile.Damage = v));
        _sidePanel.Add(new Slider("Impact Force",  0f,  30f,  _weapon.Projectile.ImpactForce,
            v => _weapon.Projectile.ImpactForce = v));

        _sidePanel.AddSeparator("Recoil");
        _sidePanel.Add(new Slider("Kick (°)",      0f,  15f,  _weapon.RecoilUp,
            v => _weapon.RecoilUp = v));
        _sidePanel.Add(new Slider("Side (°)",      0f,   5f,  _weapon.RecoilSide,
            v => _weapon.RecoilSide = v));
        _sidePanel.Add(new Slider("Recovery",      1f,  30f,  _weapon.RecoilRecovery,
            v => _weapon.RecoilRecovery = v));
    }

    // ── Enemy panel ───────────────────────────────────────────────────────────

    private void BuildEnemyPanel()
    {
        _sidePanel.Add(new Label("Name", LabelStyle.Secondary));
        var nameInput = _sidePanel.Add(new TextInput { Value = _enemy.Name, Placeholder = "enemy name" });
        nameInput.OnChange = v => _enemy.Name = v;

        _sidePanel.AddSeparator("Class");
        _sidePanel.Add(new RadioGroup(
            ["Grunt", "Heavy", "Sniper", "Gren.", "Boss"],
            (int)_enemy.Class,
            i => _enemy.Class = (EnemyClass)i));

        _sidePanel.AddSeparator("Stats");
        _sidePanel.Add(new Slider("Health",       20f, 500f, _enemy.Stats.MaxHealth,
            v => _enemy.Stats.MaxHealth = v) { Format = "F0" });
        _sidePanel.Add(new Slider("Armor",         0f, 100f, _enemy.Stats.Armor,
            v => _enemy.Stats.Armor = v));
        _sidePanel.Add(new Slider("Mass Mult",    0.5f,  4f, _enemy.Stats.MassMult));
        _sidePanel.Add(new Slider("Score",          0f, 2000f, _enemy.Stats.ScoreValue,
            v => _enemy.Stats.ScoreValue = (int)v) { Format = "F0" });

        _sidePanel.AddSeparator("AI");
        _sidePanel.Add(new RadioGroup(
            ["Patrol", "Guard", "Aggressive", "Coward"],
            (int)_enemy.AI.Behavior,
            i => _enemy.AI.Behavior = (AIBehavior)i));

        _sidePanel.Add(new Slider("Sight (m)",    2f, 30f, _enemy.AI.SightRange,
            v => _enemy.AI.SightRange = v));
        _sidePanel.Add(new Slider("Reaction (s)", 0f,  2f, _enemy.AI.ReactionTime,
            v => _enemy.AI.ReactionTime = v));
        _sidePanel.Add(new Slider("Walk (m/s)",   1f,  6f, _enemy.AI.WalkSpeed,
            v => _enemy.AI.WalkSpeed = v));
        _sidePanel.Add(new Slider("Run (m/s)",    2f, 12f, _enemy.AI.RunSpeed,
            v => _enemy.AI.RunSpeed = v));
        _sidePanel.Add(new Slider("Inaccuracy",   0f,  1f, _enemy.AI.Inaccuracy));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Resize side panel to match window height
        int scrH = Raylib.GetScreenHeight();
        _sidePanel.Bounds = _sidePanel.Bounds with { Height = scrH - 64 - 24 };

        var mouse    = Raylib.GetMousePosition();
        bool clicked  = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool held     = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool released = Raylib.IsMouseButtonReleased(MouseButton.Left);

        _modeBar.Update(mouse, clicked, held, released);
        _sidePanel.Update(mouse, clicked, held, released);

        // Status fade
        if (Raylib.GetTime() - _statusTime > 4.0)
            _statusMsg = "";
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(UITheme.WindowBg);

        // Canvas area (right of side panel)
        DrawCanvas();

        // Panels (drawn on top)
        _modeBar.Draw();
        _sidePanel.Draw();

        // Status bar
        DrawStatusBar();

        Raylib.EndDrawing();
    }

    private void DrawCanvas()
    {
        int scrW = Raylib.GetScreenWidth();
        int scrH = Raylib.GetScreenHeight();
        int canvasX = PanelW + 2;
        int canvasW = scrW - canvasX;

        // Dark canvas background
        Raylib.DrawRectangle(canvasX, 0, canvasW, scrH - 24, new Color(18, 20, 30, 255));

        // Placeholder text
        string hint = _mode switch
        {
            EditorMode.Map    => "Map canvas — tile placement coming in Phase 2",
            EditorMode.Weapon => "Weapon preview — sprite + stats coming in Phase 3",
            EditorMode.Enemy  => "Enemy preview — AI visualizer coming in Phase 4",
            _                 => ""
        };

        int tw = Raylib.MeasureText(hint, UITheme.FontSizeNormal);
        Raylib.DrawText(hint,
            canvasX + (canvasW - tw) / 2,
            scrH / 2 - UITheme.FontSizeNormal,
            UITheme.FontSizeNormal,
            new Color(60, 65, 90, 255));

        // Mode label
        string modeLabel = _mode.ToString().ToUpper() + " EDITOR";
        Raylib.DrawText(modeLabel,
            canvasX + 12, 12,
            UITheme.FontSizeMedium,
            UITheme.LabelAccent);

        // Divider
        Raylib.DrawLine(canvasX, 0, canvasX, scrH - 24, UITheme.PanelBorder);
    }

    private void DrawStatusBar()
    {
        int scrW = Raylib.GetScreenWidth();
        int scrH = Raylib.GetScreenHeight();
        int barY = scrH - 24;

        Raylib.DrawRectangle(0, barY, scrW, 24, UITheme.PanelBg);
        Raylib.DrawLine(0, barY, scrW, barY, UITheme.PanelBorder);

        if (!string.IsNullOrEmpty(_statusMsg))
        {
            Raylib.DrawText(_statusMsg,
                8, barY + (24 - UITheme.FontSizeSmall) / 2,
                UITheme.FontSizeSmall, UITheme.LabelAccent);
        }

        // Right side: data dir
        string dirInfo = $"data: {_dataDir}";
        int dw = Raylib.MeasureText(dirInfo, UITheme.FontSizeSmall);
        Raylib.DrawText(dirInfo,
            scrW - dw - 8,
            barY + (24 - UITheme.FontSizeSmall) / 2,
            UITheme.FontSizeSmall,
            UITheme.LabelSecondary);
    }

    // ── File operations ───────────────────────────────────────────────────────

    private void SaveCurrent()
    {
        try
        {
            switch (_mode)
            {
                case EditorMode.Map:
                    DataSerializer.SaveMap(_map,
                        Path.Combine(_dataDir, "maps", $"{_map.Id}.map.json"));
                    SetStatus($"Map '{_map.Name}' saved.");
                    break;
                case EditorMode.Weapon:
                    DataSerializer.SaveWeapon(_weapon,
                        Path.Combine(_dataDir, "weapons", $"{_weapon.Id}.weapon.json"));
                    SetStatus($"Weapon '{_weapon.Name}' saved.");
                    break;
                case EditorMode.Enemy:
                    DataSerializer.SaveEnemy(_enemy,
                        Path.Combine(_dataDir, "enemies", $"{_enemy.Id}.enemy.json"));
                    SetStatus($"Enemy '{_enemy.Name}' saved.");
                    break;
            }
        }
        catch (Exception ex) { SetStatus($"Error: {ex.Message}"); }
    }

    private void LoadCurrent()
    {
        // Phase 2: open a file picker overlay. For now, load newest in data dir.
        try
        {
            switch (_mode)
            {
                case EditorMode.Map:
                    var maps = DataSerializer.LoadAllMaps(Path.Combine(_dataDir, "maps"));
                    if (maps.Count > 0) { _map = maps[^1]; RebuildSidePanel(); SetStatus($"Loaded map '{_map.Name}'."); }
                    else SetStatus("No maps found.");
                    break;
                case EditorMode.Weapon:
                    var weapons = DataSerializer.LoadAllWeapons(Path.Combine(_dataDir, "weapons"));
                    if (weapons.Count > 0) { _weapon = weapons[^1]; RebuildSidePanel(); SetStatus($"Loaded weapon '{_weapon.Name}'."); }
                    else SetStatus("No weapons found.");
                    break;
                case EditorMode.Enemy:
                    var enemies = DataSerializer.LoadAllEnemies(Path.Combine(_dataDir, "enemies"));
                    if (enemies.Count > 0) { _enemy = enemies[^1]; RebuildSidePanel(); SetStatus($"Loaded enemy '{_enemy.Name}'."); }
                    else SetStatus("No enemies found.");
                    break;
            }
        }
        catch (Exception ex) { SetStatus($"Error: {ex.Message}"); }
    }

    private void NewCurrent()
    {
        switch (_mode)
        {
            case EditorMode.Map:    _map    = DataSerializer.DefaultMap();    break;
            case EditorMode.Weapon: _weapon = DataSerializer.DefaultWeapon(); break;
            case EditorMode.Enemy:  _enemy  = DataSerializer.DefaultEnemy();  break;
        }
        RebuildSidePanel();
        SetStatus("New item created.");
    }

    private void SetStatus(string msg)
    {
        _statusMsg  = msg;
        _statusTime = Raylib.GetTime();
    }
}
