using GunStrike.Core;
using GunStrike.Editor;

// ── Argument parsing ──────────────────────────────────────────────────────────
// Usage:
//   dotnet run               → launches the game
//   dotnet run -- --editor map     → map editor
//   dotnet run -- --editor weapon  → weapon editor
//   dotnet run -- --editor enemy   → enemy editor

int editorIdx = Array.IndexOf(args, "--editor");

if (editorIdx >= 0)
{
    string modeArg = (editorIdx + 1 < args.Length) ? args[editorIdx + 1].ToLowerInvariant() : "map";

    EditorMode mode = modeArg switch
    {
        "weapon" or "weapons" => EditorMode.Weapon,
        "enemy"  or "enemies" => EditorMode.Enemy,
        _                     => EditorMode.Map,
    };

    var editor = new EditorApp(mode);
    editor.Run();
}
else
{
    var game = new GameLoop();
    game.Run();
}
