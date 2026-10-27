using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tool-cards</c>: the axe's card, the drill's plain and with the arc module fitted, the nailer's and the nail's, side by side
/// over the game, as the one tooltip builds them from a stack (0098), for recording what a module changes on a card and how tiles wrap.
/// </summary>
public partial class ToolCardsDemo : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 50;
        var row = new HBoxContainer { Position = new Vector2(20, 80) };
        row.AddThemeConstantOverride("separation", 14);
        var reading = new Reading([], null, true, null, null, []);
        foreach (var stack in new[]
        {
            new Stack(Content.Weapons.Axe.Item, 1), new Stack("drill", 1), new Stack("drill", 1, Module: Content.Modules.Arc.Item),
            new Stack(Content.Shots.Nailer.Item, 1), new Stack(Content.Shots.Nail.Id, 20),
        })
            row.AddChild(ItemCardView.Build(reading.TipOf(stack).Card!));
        AddChild(row);
    }
}
