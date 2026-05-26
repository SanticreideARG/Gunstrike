using System.Numerics;
using Raylib_cs;
using GunStrike.Core;

namespace GunStrike.Input;

public record InputState(
    bool MoveLeft,
    bool MoveRight,
    bool Jump,
    bool Shoot,
    bool ToggleRagdoll,
    Vector2 MouseScreenPos,   // pixels, screen space
    Vector2 AimDirection      // normalized, world space
);

public class InputHandler
{
    public InputState Collect()
    {
        bool left     = Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left);
        bool right    = Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right);
        bool jump     = Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.W);
        bool shoot    = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool ragdoll  = Raylib.IsKeyPressed(KeyboardKey.R);

        var mouseScreen = Raylib.GetMousePosition();

        // AimDirection will be resolved by the player once it knows its screen position
        return new InputState(left, right, jump, shoot, ragdoll, mouseScreen, Vector2.Zero);
    }
}
