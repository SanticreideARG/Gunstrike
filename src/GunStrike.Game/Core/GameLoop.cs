using System.Numerics;
using Raylib_cs;
using GunStrike.Physics;
using GunStrike.Rendering;
using GunStrike.Input;
using GunStrike.Entities;

namespace GunStrike.Core;

public class GameLoop
{
    private readonly PhysicsWorld      _physics;
    private readonly GameCamera        _camera;
    private readonly InputHandler      _input;
    private readonly PlayerEntity      _player;
    private readonly LevelRenderer     _level;
    private readonly ParallaxSystem    _parallax;
    private readonly ProjectileManager _projectiles;
    private readonly EnemyManager      _enemies;

    public GameLoop()
    {
        _physics  = new PhysicsWorld();
        _input    = new InputHandler();
        _level    = new LevelRenderer(_physics);
        _parallax = new ParallaxSystem(MapEnvironment.Forest);

        // Ground top = 14.5 - 0.5 = 14.0m. Feet bottom = torso.Y + 0.84m → torso.Y = 13.16m
        var spawnMeters = new Vector2(5f, 13.0f);
        var spawnPixels = spawnMeters * GameConstants.PhysicsToPixels;
        _camera      = new GameCamera(spawnPixels);
        _player      = new PlayerEntity(_physics, spawnMeters);
        _projectiles = new ProjectileManager(_physics, _player);
        _enemies     = new EnemyManager(_physics);
    }

    public void Run()
    {
        Raylib.InitWindow(GameConstants.ScreenWidth, GameConstants.ScreenHeight, GameConstants.Title);
        Raylib.SetTargetFPS(GameConstants.TargetFPS);
        Raylib.HideCursor();

        Load();

        int frameCount = 0;
        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            Update(dt);
            Draw();

            frameCount++;
            if (frameCount == 120)
                Raylib.TakeScreenshot("screenshot_auto.png");

            if (frameCount >= 90 && System.Environment.GetEnvironmentVariable("AUTOCLOSE") == "1")
                break;
        }

        Unload();
        Raylib.ShowCursor();
        Raylib.CloseWindow();
    }

    private void Load()
    {
        _level.Load();
        _player.Load();

        // Spawn a few test enemies at hardcoded positions
        var grunt = new GunStrike.Data.EnemyData { Name = "Grunt", Class = GunStrike.Data.EnemyClass.Grunt };
        grunt.AI.SightRange     = 12f;
        grunt.AI.AttackRange    =  6f;
        grunt.AI.WalkSpeed      =  2.5f;
        grunt.AI.RunSpeed       =  5f;
        grunt.AI.ReactionTime   =  0.4f;
        grunt.AI.Inaccuracy     =  0.35f;
        grunt.AI.AttackInterval =  1.2f;

        _enemies.Spawn(grunt, new Vector2(15f, 13.0f), patrolRange: 3f);
        _enemies.Spawn(grunt, new Vector2(25f, 13.0f), patrolRange: 4f);

        var heavy = new GunStrike.Data.EnemyData { Name = "Heavy", Class = GunStrike.Data.EnemyClass.Heavy };
        heavy.AI.SightRange      =  8f;
        heavy.AI.AttackRange     =  4f;
        heavy.AI.WalkSpeed       =  1.5f;
        heavy.AI.RunSpeed        =  3f;
        heavy.AI.ReactionTime    =  0.6f;
        heavy.AI.Inaccuracy      =  0.2f;
        heavy.AI.AttackInterval  =  0.8f;
        heavy.Stats.MaxHealth    = 200f;
        heavy.Stats.AttackDamage =  40f;

        _enemies.Spawn(heavy, new Vector2(30f, 13.0f), patrolRange: 2f);

        // Connect bullet → enemy hits
        _projectiles.OnEnemyHit += (body, pt, dmg) => _enemies.ApplyBulletHit(body, pt, dmg);
        // Connect enemy hitscan → player damage
        _enemies.OnPlayerHit += dmg => _player.TakeDamage(dmg);
    }

    private void Update(float dt)
    {
        var rawInput = _input.Collect();

        // ── Convert mouse screen → world pixels → aim direction ─────────────────
        var mouseWorldPx  = _camera.ScreenToWorld(rawInput.MouseScreenPos);
        var aimDirPx      = mouseWorldPx - _player.PixelPosition;
        var aimDirNorm    = aimDirPx.LengthSquared() > 1f
                            ? Vector2.Normalize(aimDirPx)
                            : Vector2.UnitX;

        // Rebuild input with world-space aim direction
        var input = rawInput with { AimDirection = aimDirNorm };

        _player.Update(dt, input);
        _projectiles.Update(dt);
        _physics.Step(dt);
        _camera.Follow(_player.PixelPosition);
        _enemies.Update(dt, _player.TorsoMeters, _player.IsAlive);

        // ── Shoot ────────────────────────────────────────────────────────────────
        if (input.Shoot && !_player.IsRagdoll && !_player.IsReloading)
            _projectiles.Shoot(_player.MuzzleMeters, aimDirNorm);
    }

    private void Draw()
    {
        Raylib.BeginDrawing();

        // ── Plano 1 + 2 (screen space, no camera) ────────────────────────────────
        float camLeft = _camera.RaylibCamera.Target.X - GameConstants.ScreenWidth  / 2f;
        _parallax.DrawBackground(camLeft);

        // ── Plano 3: mapa + entidades (world space via camera) ───────────────────
        Raylib.BeginMode2D(_camera.RaylibCamera);

            _level.Draw();
            _projectiles.Draw();
            _player.Draw();
            _enemies.Draw();

        Raylib.EndMode2D();

        // ── Screen-space overlays ────────────────────────────────────────────────
        var camTargetPx = new Vector2(_camera.RaylibCamera.Target.X,
                                      _camera.RaylibCamera.Target.Y);
        var screenCenter = new Vector2(GameConstants.ScreenWidth  / 2f,
                                       GameConstants.ScreenHeight / 2f);
        _projectiles.DrawMuzzleFlash(camTargetPx, screenCenter);

        DrawHUD();
        DrawCrosshair();

        Raylib.EndDrawing();
    }

    private void DrawHUD()
    {
        Raylib.DrawFPS(10, 10);

        string modeText  = _player.IsRagdoll ? "[ RAGDOLL ]" : "[  ACTIVO  ]";
        Color  modeColor = _player.IsRagdoll ? Color.Red : Color.Lime;
        Raylib.DrawText(modeText, 10, 36, 18, modeColor);

        // Bullet counter
        Raylib.DrawText($"Proyectiles: {_projectiles.ActiveCount}",
                        10, 60, 14, new Color(200, 200, 200, 200));

        // ── Reload bar ───────────────────────────────────────────────────────
        if (_player.IsReloading)
        {
            const int barW = 220, barH = 10;
            int bx = (GameConstants.ScreenWidth  - barW) / 2;
            int by = GameConstants.ScreenHeight  - 56;

            // Label
            string label = "RECARGANDO";
            int lw = Raylib.MeasureText(label, 15);
            Raylib.DrawText(label,
                (GameConstants.ScreenWidth - lw) / 2, by - 22,
                15, Color.Yellow);

            // Track
            Raylib.DrawRectangle(bx, by, barW, barH, new Color(40, 40, 40, 210));
            // Fill
            int fillW = (int)(barW * _player.ReloadProgress);
            Raylib.DrawRectangle(bx, by, fillW, barH, Color.Yellow);
            // Border
            Raylib.DrawRectangleLines(bx, by, barW, barH, new Color(200, 200, 200, 180));
        }

        // Health bar
        float hpFrac = Math.Clamp(_player.Health / _player.MaxHealth, 0f, 1f);
        int hbX = 10, hbY = GameConstants.ScreenHeight - 48;
        int hbW = 160, hbH = 12;
        Raylib.DrawRectangle(hbX, hbY, hbW, hbH, new Color(40, 20, 20, 200));
        Raylib.DrawRectangle(hbX, hbY, (int)(hbW * hpFrac), hbH,
            hpFrac > 0.5f ? new Color(50, 200, 80, 255)
            : hpFrac > 0.25f ? new Color(200, 180, 50, 255)
            : new Color(200, 50, 50, 255));
        Raylib.DrawRectangleLines(hbX, hbY, hbW, hbH, new Color(180, 180, 180, 160));
        Raylib.DrawText($"HP  {(int)_player.Health}", hbX + hbW + 8, hbY + (hbH - 13) / 2, 13,
            new Color(200, 200, 200, 200));

        // Enemy count
        Raylib.DrawText($"Enemies: {_enemies.AliveCount}/{_enemies.TotalCount}",
            10, hbY - 18, 13, new Color(200, 200, 200, 200));

        // Control hints
        string hint = _player.IsReloading
            ? "Recargando…  (T: ragdoll)"
            : "A/D: mover  |  SPACE: saltar  |  LMB: disparar  |  R: recargar  |  T: ragdoll";
        Raylib.DrawText(hint,
            10, GameConstants.ScreenHeight - 22, 13, new Color(200, 200, 200, 180));
    }

    private static void DrawCrosshair()
    {
        var m = Raylib.GetMousePosition();
        float s = 10f;
        Raylib.DrawLineEx(new Vector2(m.X - s, m.Y), new Vector2(m.X + s, m.Y), 1.5f, Color.White);
        Raylib.DrawLineEx(new Vector2(m.X, m.Y - s), new Vector2(m.X, m.Y + s), 1.5f, Color.White);
        Raylib.DrawCircleLines((int)m.X, (int)m.Y, 5, new Color(255, 255, 255, 160));
    }

    private void Unload()
    {
        _player.Unload();
        _level.Unload();
    }
}
