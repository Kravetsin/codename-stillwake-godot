using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a tool works while it is used (<see cref="AirCuts"/>, tool-reach): a swing's blur, left in the world where it was swept,
/// a thrust's dust, a running drill's
/// or grinder's dry-brush blur over its zone. Drawn in the arms' frame, the shoulder at its origin, so a figure facing left mirrors
/// it; pale and unshaded, added over the figure.
/// </summary>
public partial class ReachAir : Node2D
{
    private readonly AirCuts _cuts = new();
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();

    public ReachAir()
    {
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Visible = false;
    }

    /// <summary>A thrust begins cutting the air now (<see cref="AirCuts.Thrust"/>).</summary>
    public void Thrust(double aim, double half, double range, double inner, double sweepMs, double fadeMs) =>
        _cuts.Thrust(aim, half, range, inner, sweepMs, fadeMs);

    /// <summary>A swing's blade is cutting now (<see cref="AirCuts.Swing"/>).</summary>
    public void Swing(double from, double to, double range)
    {
        var world = GetGlobalTransform();
        var toward = world.BasisXform(new Vector2((float)Math.Cos(to), (float)Math.Sin(to)));
        _cuts.Swing(from, to, range, (world.Origin.X, world.Origin.Y), (toward.X, toward.Y));
    }

    /// <summary><paramref name="elapsedMs"/> on, the running tool's <paramref name="zone"/>, none while none runs.</summary>
    public void Follow(AirCuts.Zone? zone, double elapsedMs)
    {
        _cuts.Step(elapsedMs, zone);
        Visible = _cuts.Live;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        _ink.Clear();
        _ink.Feather = InkCanvas.PixelOf(this);
        var local = GetGlobalTransform().AffineInverse();
        _cuts.Draw(_ink, p => { var l = local * new Vector2((float)p.X, (float)p.Y); return (l.X, l.Y); });
        _canvas.Draw(this, _ink);
    }
}
