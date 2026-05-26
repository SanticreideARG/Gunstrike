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

        // ── Shoot ────────────────────────────────────────────────────────────────
        if (input.Shoot && !_player.IsRagdoll)
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

        Raylib.DrawText(
            "A/D: mover  |  SPACE: saltar  |  LMB: disparar  |  R: ragdoll",
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
