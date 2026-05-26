using System.Numerics;
using Raylib_cs;
using nkast.Aether.Physics2D.Dynamics;
using GunStrike.Core;
using GunStrike.Physics;

namespace GunStrike.Entities;

public enum BodyPartId
{
    Torso, Head,
    UpperArmR, LowerArmR,
    UpperArmL, LowerArmL,
    UpperLegR, LowerLegR,
    UpperLegL, LowerLegL
}

/// <summary>
/// One physical segment of the ragdoll character.
/// </summary>
public class BodyPart
{
    public BodyPartId Id       { get; }
    public Body       Body     { get; }
    public float      Width    { get; }   // meters
    public float      Height   { get; }   // meters
    public Color      DrawColor { get; }

    public BodyPart(BodyPartId id, Body body, float width, float height, Color color)
    {
        Id        = id;
        Body      = body;
        Width     = width;
        Height    = height;
        DrawColor = color;
    }

    /// <summary>World-space pixel position of body center (System.Numerics.Vector2).</summary>
    public Vector2 PixelPosition =>
        PhysicsWorld.ToPixels(new Vector2(Body.Position.X, Body.Position.Y));

    /// <summary>Rotation in radians.</summary>
    public float Rotation => Body.Rotation;

    public void Draw()
    {
        float pw = Width  * GameConstants.PhysicsToPixels;
        float ph = Height * GameConstants.PhysicsToPixels;

        var pos  = PixelPosition;
        var rect = new Rectangle(pos.X, pos.Y, pw, ph);
        var orig = new Vector2(pw / 2f, ph / 2f);
        float deg = Body.Rotation * (180f / MathF.PI);

        Raylib.DrawRectanglePro(rect, orig, deg, DrawColor);
        Raylib.DrawCircleV(pos, 2f, Color.White);  // debug pivot
    }
}
