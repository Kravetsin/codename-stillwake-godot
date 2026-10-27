using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --cape</c>: every hero's figure in the cape (a prototype, <see cref="Cape"/>), put on as figures appear; the
/// <see cref="HeroAutopilot.CapeRun"/> runs the hero about the hall with the axe and then the drill, pressing the
/// hotbar keys it takes as a player would, or the keys do with <c>--manual</c>.
/// </summary>
public partial class CapeDemo(HeroAutopilot? script) : Node
{
    private double _sinceLook = 1;

    public override void _Process(double delta)
    {
        if (script?.TakeSlot() is int slot)
        {
            var key = Key.Key0 + slot;
            foreach (bool pressed in new[] { true, false })
                Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });
        }
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
