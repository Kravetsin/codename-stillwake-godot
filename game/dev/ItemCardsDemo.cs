using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --item-cards</c>: every food's card and the HUD's ration with 4:13 left, every piece of gear's, and the fire axe's beside
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
        foreach (var food in Content.Foods.All)
            if (food.Id != Content.Foods.Paste) Add(cards, reading.TipOf(new Stack(food.Id, 1)).Card!);
        Add(cards, MealTips.Of(Content.Foods.Ration, (4 * 60 + 13) * Sim.World.TicksPerSecond).Card!);
        Add(cards, MealTips.Of(Content.Foods.Paste, 600).Card!);
        string[] gear =
        [
            Content.Helmet.Basic, Content.Helmet.Quilted, Content.Helmet.Heavy, Content.Suit.Basic, Content.Suit.Regen, Content.Cape.Item,
            Content.Jetpack.Item, Content.HelmetLamp.Item, Content.Helmet.Scanner, "battery", Content.Weapons.Axe.Item,
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
