using Godot;

namespace Stillwake.Game.Features.Creatures;

/// <summary>A creature whose rig draws it (<see cref="InkRig"/>): turned about its middle, a height over its feet.</summary>
public abstract partial class PosedSprite(float middle) : Node2D
{
    /// <summary>At <paramref name="feet"/>, shaken across, turned about its middle, facing 1 right or −1 left.</summary>
    protected void Place(Vector2 feet, double shake, double angle, int face, float size = 1)
    {
        Position = feet + new Vector2((float)shake, -middle * size);
        Rotation = (float)angle;
        Scale = new Vector2(face * size, size);
    }
}
