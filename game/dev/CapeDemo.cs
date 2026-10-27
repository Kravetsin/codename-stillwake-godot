using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --cape</c>: every hero's figure in the cape (a prototype, <see cref="Cape"/>), put on as figures appear; the
/// <see cref="HeroAutopilot.CapeRun"/> runs the hero about the hall, or the keys do with <c>--manual</c>.
/// </summary>
public partial class CapeDemo : Node
{
    private double _sinceLook = 1;

    public override void _Process(double delta)
    {
        _sinceLook += delta;
        if (_sinceLook < 0.5) return;
        _sinceLook = 0;
        Wear(GetTree().Root);
    }

    private static void Wear(Node node)
    {
        if (node is Figure figure) figure.WearsCape = true;
        foreach (var child in node.GetChildren()) Wear(child);
    }
}
