namespace GunStrike.Core;

public static class GameConstants
{
    // ── Window ──────────────────────────────────────────────────────────────────
    public const int    ScreenWidth  = 1280;
    public const int    ScreenHeight = 720;
    public const string Title        = "GunStrike";
    public const int    TargetFPS    = 60;

    // ── Physics scale ───────────────────────────────────────────────────────────
    // Character visual height target = 275 px
    // Character physics height ≈ 1.375 m  →  200 px/m
    public const float PhysicsToPixels = 200f;
    public const float PixelsToPhysics = 1f / PhysicsToPixels;

    // ── Character visual dimensions (pixels) ────────────────────────────────────
    public const float CharacterHeightPx = 275f;  // reference — segments sum to this

    // ── Character physics dimensions (meters) ───────────────────────────────────
    // head(0.25) + torso(0.52) + upperLeg(0.30) + lowerLeg(0.28) = 1.35m → 270px
    // (gaps at joints bring it to ~275px visually)
    public const float SegHead      = 0.25f;   // m  →  50 px
    public const float SegTorso     = 0.52f;   // m  → 104 px
    public const float SegUpperArm  = 0.27f;   // m  →  54 px
    public const float SegLowerArm  = 0.24f;   // m  →  48 px
    public const float SegUpperLeg  = 0.30f;   // m  →  60 px
    public const float SegLowerLeg  = 0.28f;   // m  →  56 px

    // ── World / Physics ─────────────────────────────────────────────────────────
    public const float GravityX = 0f;
    public const float GravityY = 22f;     // m/s² — snappy platformer feel

    // ── Player movement ─────────────────────────────────────────────────────────
    public const float PlayerMoveSpeed   = 5.5f;   // m/s
    public const float PlayerJumpImpulse = 11f;    // m/s upward
    public const float PlayerMaxFallSpeed = 28f;

    // ── Ragdoll damping ─────────────────────────────────────────────────────────
    public const float RagdollLinearDamping  = 0.4f;
    public const float RagdollAngularDamping = 0.8f;

    // ── Projectiles ─────────────────────────────────────────────────────────────
    public const float BulletSpeed    = 60f;    // m/s
    public const float BulletLifespan = 3f;     // seconds

    // ── Parallax scroll factors ─────────────────────────────────────────────────
    // Layer 3 (map)        = 1.00  →  moves 1:1 with camera (normal)
    // Layer 2 (mid/dunes)  = 0.20  →  20% of layer 3
    // Layer 1 (sky)        = 0.04  →  20% of layer 2  (≈ 4% of map)
    public const float ParallaxSky = 0.04f;
    public const float ParallaxMid = 0.20f;
    public const float ParallaxMap = 1.00f;
}
