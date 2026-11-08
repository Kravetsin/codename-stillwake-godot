using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a tool works while it is used (<see cref="AirCuts"/>, tool-reach): a stroke's dust carried round its fan, a running drill's
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

    /// <summary>A stroke begins cutting the air now (<see cref="AirCuts.Strike"/>).</summary>
    public void Strike(bool thrust, double aim, double half, double range, double inner, double sweepMs, double fadeMs) =>
        _cuts.Strike(thrust, aim, half, range, inner, sweepMs, fadeMs);

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
        _cuts.Draw(_ink);
        _canvas.Draw(this, _ink);
    }
}
