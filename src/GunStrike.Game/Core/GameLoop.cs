using System.Numerics;
using Raylib_cs;
using GunStrike.Physics;
using GunStrike.Rendering;
using GunStrike.Input;
using GunStrike.Entities;

namespace GunStrike.Core;

public class GameLoop
{
    private readonly PhysicsWorld    _physics;
    private readonly GameCamera      _camera;
    private readonly InputHandler    _input;
    private readonly PlayerEntity    _player;
    private readonly LevelRenderer   _level;
    private readonly ParallaxSystem  _parallax;

    public GameLoop()
    {
        _physics  = new PhysicsWorld();
        _input    = new InputHandler();
        _camera   = new GameCamera();
        _level    = new LevelRenderer(_physics);
        _parallax = new ParallaxSystem(MapEnvironment.Forest);

        // Spawn at (5m, 10m) — above ground level (ground center at 14.5m)
        _player = new PlayerEntity(_physics, new Vector2(5f, 10f));
    }

    public void Run()
    {
        Raylib.InitWindow(GameConstants.ScreenWidth, GameConstants.ScreenHeight, GameConstants.Title);
        Raylib.SetTargetFPS(GameConstants.TargetFPS);

        Load();

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            Update(dt);
            Draw();
        }

        Unload();
        Raylib.CloseWindow();
    }

    private void Load()
    {
        _level.Load();
        _player.Load();
    }

    private void Update(float dt)
    {
        var input = _input.Collect();
        _player.Update(dt, input);
        _physics.Step(dt);
        _camera.Follow(_player.PixelPosition);
    }

    private void Draw()
    {
        Raylib.BeginDrawing();

        // ── Plano 1 + Plano 2 : sin transformación de cámara ────────────────────
        // cameraWorldX = left edge of view in world pixels
        float camLeft = _camera.RaylibCamera.Target.X - GameConstants.ScreenWidth / 2f;
        _parallax.DrawBackground(camLeft);

        // ── Plano 3 : mapa + entidades dentro de la cámara 2D ───────────────────
        Raylib.BeginMode2D(_camera.RaylibCamera);

            _level.Draw();
            _player.Draw();

        Raylib.EndMode2D();

        // ── HUD (screen space) ───────────────────────────────────────────────────
        DrawHUD();

        Raylib.EndDrawing();
    }

    private void DrawHUD()
    {
        Raylib.DrawFPS(10, 10);

        string modeText = _player.IsRagdoll ? "[ RAGDOLL ]" : "[  ACTIVO  ]";
        Color  modeColor = _player.IsRagdoll ? Color.Red : Color.Lime;
        Raylib.DrawText(modeText, 10, 36, 18, modeColor);

        // Crosshair en el cursor
        var mouse = Raylib.GetMousePosition();
        Raylib.DrawLineEx(new Vector2(mouse.X - 10, mouse.Y), new Vector2(mouse.X + 10, mouse.Y), 1.5f, Color.White);
        Raylib.DrawLineEx(new Vector2(mouse.X, mouse.Y - 10), new Vector2(mouse.X, mouse.Y + 10), 1.5f, Color.White);
        Raylib.DrawCircleLines((int)mouse.X, (int)mouse.Y, 6, Color.White);

        Raylib.DrawText(
            "A/D: mover  |  SPACE: saltar  |  MOUSE: apuntar  |  R: ragdoll",
            10, GameConstants.ScreenHeight - 22, 13, new Color(200, 200, 200, 180));
    }

    private void Unload()
    {
        _player.Unload();
        _level.Unload();
    }
}
