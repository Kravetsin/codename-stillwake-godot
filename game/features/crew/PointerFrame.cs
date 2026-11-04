using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A builder's frame standing in for the pointer over the world (pointer-looks): the cells the next press works, teal and
/// breathing, grey out of reach; while the power driver's square waits for charge, the cell and the square dotted round it.
/// </summary>
public partial class PointerFrame : Node2D
{
    private static readonly Color OutOfReach = new("8a94a6");
    /// <summary>How strongly the frame's cells are washed with its colour, and its dotted square drawn.</summary>
    private const float Wash = 0.12f, Dots = 0.6f;

    private ((int X, int Y, int Side) Solid, (int X, int Y, int Side)? Dotted, bool Reach)? _shown;

    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>Over every hero, as the ghost of a card in hand, and unlit: it is the pointer, read in the dark too.</summary>
    public PointerFrame()
    {
        ZIndex = Layers.PlanGhost;
        Material = Unlit;
    }

    /// <summary>Shows <paramref name="frame"/>, in reach or not; none hides it.</summary>
    public void Show(((int X, int Y, int Side) Solid, (int X, int Y, int Side)? Dotted)? frame, bool reach)
    {
        ((int X, int Y, int Side) Solid, (int X, int Y, int Side)? Dotted, bool Reach)? shown = null;
        if (frame is { } f) shown = (f.Solid, f.Dotted, reach);
        Visible = shown != null;
        if (shown == _shown) return;
        _shown = shown;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (Visible) SelfModulate = Colors.White with { A = (float)Breath.Alpha(Time.GetTicksMsec()) };
    }

    public override void _Draw()
    {
        if (_shown is not { } s) return;
        var colour = s.Reach ? Outline.SquareReady : OutOfReach;
        static Rect2 Box((int X, int Y, int Side) b) =>
            new(b.X * ShipCamera.Cell, b.Y * ShipCamera.Cell, b.Side * ShipCamera.Cell, b.Side * ShipCamera.Cell);
        var solid = Box(s.Solid);
        DrawRect(solid, colour with { A = Wash });
        DrawRect(solid.Grow(-0.5f), colour, filled: false, width: 1);
        if (s.Dotted is not { } dotted) return;
        var r = Box(dotted).Grow(-0.5f);
        Vector2 a = r.Position, b = new(r.End.X, r.Position.Y), c = r.End, d = new(r.Position.X, r.End.Y);
        foreach (var (from, to) in new[] { (a, b), (b, c), (c, d), (d, a) }) DrawDashedLine(from, to, colour with { A = Dots }, 1, 1.5f);
    }
}
