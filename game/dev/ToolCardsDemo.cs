using System.Linq;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tool-cards</c>: every hand tool's card, the drill's plain and with the arc module fitted, the nailer's and the railgun's,
/// in rows over the game, as the one tooltip builds them from a stack (0098), for recording what a module changes on a card and
/// how its rows read (2026-11-07-tool-card-rows); under them every tool in a slot, for their icons (2026-10-27-tool-icons).
/// </summary>
public partial class ToolCardsDemo : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 50;
        var cards = new HFlowContainer { Position = new Vector2(8, 6), Size = new Vector2(1264, 0) };
        cards.AddThemeConstantOverride("h_separation", 10);
        cards.AddThemeConstantOverride("v_separation", 8);
        var reading = new Reading([], null, true, null);
        Stack[] stacks =
        [
            .. Content.Weapons.All.Select(w => new Stack(w.Item, 1)),
            new Stack("drill", 1, Module: Content.Modules.Arc.Item),
            new Stack(Content.Shots.Nailer.Item, 1), new Stack(Content.Shots.Railgun.Item, 1),
        ];
        foreach (var stack in stacks)
        {
            var card = ItemCardView.Build(reading.TipOf(stack).Card!);
            card.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            cards.AddChild(card);
        }
        AddChild(cards);
        string[] items = [.. Content.Items.All.Select(i => i.Id).Where(i => HeldLook.Of(i) != null), "scrap", "battery", Content.Shots.Nail.Id];
        var slots = new HBoxContainer { Position = new Vector2(8, 720 - Slots.SizeOf(SlotScale.Normal) - 6) };
        slots.AddThemeConstantOverride("separation", Slots.Gap);
        for (int i = 0; i < items.Length; i++)
        {
            var box = new SlotBox(new Place(i), Slots.SizeOf(SlotScale.Normal));
            slots.AddChild(box);
            box.Show(new Stack(items[i], 1), chosen: i == 0, picked: false, hovered: false, SlotMark.None);
        }
        AddChild(slots);
    }
}
