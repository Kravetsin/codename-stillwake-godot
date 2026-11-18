using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --item-cards</c>: a canister's and a bottle's cards, every food's card and the HUD's ration with 4:13 left, every piece of gear's, and the fire axe's beside
/// them, as the one tooltip builds them from a stack (2026-11-14-food-and-gear-cards), scrolled with the wheel.
/// </summary>
public partial class ItemCardsDemo : CanvasLayer
{
    public override void _Ready()
    {
        Layer = 50;
        var scroll = new ScrollContainer { Position = new Vector2(8, 6), Size = new Vector2(1264, 708) };
        var cards = new HFlowContainer { CustomMinimumSize = new Vector2(1250, 0) };
        cards.AddThemeConstantOverride("h_separation", 10);
        cards.AddThemeConstantOverride("v_separation", 8);
        var reading = new Reading([], null, true, null);
        // The tools first, their pictures enlarged (2026-11-14-tool-card-pictures): long, tall and the smallest.
        foreach (var tool in (string[])[Content.Shots.Railgun.Item, Content.Weapons.Drill.Item, Content.Grapple.Item, "pickaxe", "screwdriver"])
            Add(cards, reading.TipOf(new Stack(tool, 1)).Card!);
        // A canister and a bottle, as food's cards (liquid-volumes): one, and stacks with their litres all told.
        foreach (var (item, count) in ((string, int)[])[(Content.Crew.Canister, 1), (Content.Crew.Canister, 4), (Content.Water.Item, 5)])
            Add(cards, reading.TipOf(new Stack(item, count)).Card!);
        foreach (var food in Content.Foods.All)
            Add(cards, reading.TipOf(new Stack(food.Id, 1)).Card!);
        Add(cards, MealTips.Of(Content.Foods.Ration, (4 * 60 + 13) * Sim.World.TicksPerSecond).Card!);
        string[] gear =
        [
            Content.Helmet.Basic, Content.Helmet.Heated, Content.Helmet.HeatProof, Content.Helmet.Heavy, Content.Suit.Basic, Content.Suit.Regen, Content.Suit.Heat, Content.Cape.Item,
            Content.Jetpack.Item, Content.LightDrone.Item, Content.Helmet.Scanner, "battery", Content.Weapons.Axe.Item,
        ];
        foreach (var item in gear) Add(cards, reading.TipOf(new Stack(item, 1)).Card!);
        scroll.AddChild(cards);
        AddChild(scroll);
    }

    private static void Add(HFlowContainer cards, ItemCard card)
    {
        var built = ItemCardView.Build(card);
        built.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        cards.AddChild(built);
    }
}
