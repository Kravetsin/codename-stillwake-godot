using Godot;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --poses</c> (0090): the <see cref="HeroAutopilot.Hall"/> walked by <see cref="HeroAutopilot.PosesRun"/>, the hero
/// carrying the axe and the drill (<see cref="GroundDemo.Seed"/>); the hotbar keys the script takes pressed as a player's
/// would, for recording the figure's motion with empty hands and with a two-handed item held.
/// </summary>
public partial class PosesDemo(HeroAutopilot script) : Node
{
    public override void _Process(double delta)
    {
        if (script.TakeSlot() is not int slot) return;
        var key = Key.Key0 + slot;
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });
    }
}
