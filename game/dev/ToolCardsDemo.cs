using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tool-cards</c>: the drill's card plain and with the arc module fitted, side by side over the game, as the one tooltip
/// builds them from a stack (0098), for recording what a module changes on a card.
/// </summary>
public partial class ToolCardsDemo : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 50;
        var row = new HBoxContainer { Position = new Vector2(80, 80) };
        row.AddThemeConstantOverride("separation", 40);
        var reading = new Reading([], null, true, null, null, []);
        foreach (var stack in new[] { new Stack("drill", 1), new Stack("drill", 1, Module: Content.Modules.Arc.Item) })
            row.AddChild(ItemCardView.Build(reading.TipOf(stack).Card!));
        AddChild(row);
    }
}
