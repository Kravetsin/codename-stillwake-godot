using System.Linq;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tool-cards</c>: the axe's card, the drill's plain and with the arc module fitted, the nailer's, the railgun's and the nail's,
/// side by side over the game, as the one tooltip builds them from a stack (0098), for recording what a module changes on a card and how
/// tiles wrap; under them every tool in a slot at both sizes, and some other items, for their icons (2026-10-27-tool-icons).
/// </summary>
public partial class ToolCardsDemo : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 50;
        var row = new HBoxContainer { Position = new Vector2(20, 80) };
        row.AddThemeConstantOverride("separation", 14);
        var reading = new Reading([], null, true, null);
        foreach (var stack in new[]
        {
            new Stack(Content.Weapons.Axe.Item, 1), new Stack("drill", 1), new Stack("drill", 1, Module: Content.Modules.Arc.Item),
            new Stack(Content.Shots.Nailer.Item, 1), new Stack(Content.Shots.Railgun.Item, 1), new Stack(Content.Shots.Nail.Id, 20),
        })
            row.AddChild(ItemCardView.Build(reading.TipOf(stack).Card!));
        AddChild(row);
        string[] items = [.. Content.Items.All.Select(i => i.Id).Where(i => HeldLook.Of(i) != null), "scrap", "battery", Content.Shots.Nail.Id];
        float top = 520;
        foreach (var scale in new[] { SlotScale.Normal, SlotScale.Large })
        {
            var slots = new HBoxContainer { Position = new Vector2(20, top) };
            slots.AddThemeConstantOverride("separation", Slots.Gap);
            for (int i = 0; i < items.Length; i++)
            {
                var box = new SlotBox(new Place(i), Slots.SizeOf(scale));
                slots.AddChild(box);
                box.Show(new Stack(items[i], 1), chosen: i == 0, picked: false, hovered: false, SlotMark.None);
            }
            AddChild(slots);
            top += Slots.SizeOf(scale) + 12;
        }
    }
}
