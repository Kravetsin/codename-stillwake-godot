using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Creatures;

/// <summary>One breaker's painted pieces (breakers-look), its origin its middle: at work on a wreck, or a body flying (<see cref="BreakerBodies"/>).</summary>
public partial class BreakerSprite : PosedSprite
{
    private readonly BreakerRig _rig;
    private readonly InkRig _ink;

    public BreakerKind Kind { get; }

    public BreakerSprite(BreakerKind kind) : base(0)
    {
        Kind = kind;
        _rig = new BreakerRig(kind.Design, kind.Moves);
        _ink = new InkRig(kind.Design.Id, kind.Design.Parts);
        AddChild(_ink);
    }

    public void Show(in BreakerPose pose, double flash)
    {
        _rig.Place(pose);
        _ink.Show(_rig.Slots, flash, 0);
    }

    /// <summary>Its middle at <paramref name="middle"/>, turned, facing <paramref name="side"/> (a share of it as it turns round).</summary>
    public void Lies(Vector2 middle, double shake, double angle, double side)
    {
        Place(middle, shake, angle, 1);
        Scale = new Vector2((float)side, 1);
    }

    /// <summary>Under the debris, as one still coming out from beneath its piece, or where every creature is.</summary>
    public void Beneath(bool under)
    {
        ZAsRelative = !under;
        ZIndex = under ? Layers.UnderDebris : 0;
    }
}
