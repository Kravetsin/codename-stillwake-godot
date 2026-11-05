using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The trail a swung tool's downstroke leaves (<see cref="SwingTrail"/>, damage-looks): three pale ink flicks after the blade,
/// fading after the stroke. Drawn in the arms' frame, the shoulder at its origin, so a figure facing left mirrors it; added over the figure.
/// </summary>
public partial class SwingSmear : Node2D
{
    private readonly SwingTrail _trail = new();
    private readonly Ink _ink = new();
    private readonly InkCanvas _canvas = new();

    public SwingSmear()
    {
        // Pale ink over the figure, not darkened with the scene: unshaded, its own colour.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        Visible = false;
    }

    /// <summary>
    /// The trail <paramref name="elapsedMs"/> on: the blade's edge from <paramref name="from"/> to <paramref name="to"/>, pixels from
    /// the shoulder, while a tool is swung (none else), on its downstroke of <paramref name="downMs"/> while <paramref name="striking"/>.
    /// </summary>
    public void Follow((double X, double Y)? from, (double X, double Y)? to, bool striking, double downMs, double elapsedMs)
    {
        _trail.Step(elapsedMs, from, to, striking, downMs);
        Visible = _trail.Light > 0;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        _ink.Clear();
        _ink.Feather = InkCanvas.PixelOf(this);
        _trail.Draw(_ink);
        _canvas.Draw(this, _ink);
    }
}
